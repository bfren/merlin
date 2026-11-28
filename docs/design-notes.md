# Merlin — design notes

Nov 23, 2026 · @Ben Green

## Goals and principles

Merlin is a small, Hono-like web framework for .NET where a handler is a function from a request to a `Result<Reply>`. It owns routing, binding, replies and failure handling; Kestrel owns HTTP.

- **Result-native.** Handlers, binders and reply writes all return Wrap's `Result<T>`, so a request composes as one `from … select` chain.
- **Functional.** Immutable records for `Req`, `Reply` and templates; binders are extension methods; no hidden state in handlers.
- **AOT-safe, no reflection.** Route values parse through `IParsable<T>`; JSON and forms go through source-generated `JsonTypeInfo<T>`. `IsAotCompatible` is on in the library, `PublishAot` in apps.
- **Kestrel underneath, isolated.** `HttpContext` appears in one adapter; everything else talks to `Req` and `Reply`, so the engine stays swappable.
- **Own the interesting parts.** Routing and binding are written by hand, for learning; the security-critical HTTP layer is not.
- **Fail at startup, not at runtime.** Bad templates and duplicate routes throw during registration; runtime problems are `Failure`s.

## Architecture overview

A request passes through one adapter, which matches a route, builds a `Req`, runs the handler and writes the `Reply`. `HttpContext` never reaches handler code except through `Req`'s escape hatch.

&#91;embedded content: request flow · Kestrel to reply\]

Not-found and method-not-allowed are ordinary replies from the router; only a `Failure` anywhere in the handler or write chain goes to logging and the error reply.

## Hosting: App and the Kestrel adapter

`App` builds a slim `WebApplication`, owns the `Router` and the JSON options, and runs one adapter that turns an `HttpContext` into a `Req`, runs the matched handler and writes the `Reply`.

**Project setup**

- Library (`Merlin.csproj`): plain `Microsoft.NET.Sdk`, `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, `<IsAotCompatible>true</IsAotCompatible>`. No `Microsoft.AspNetCore.*` NuGet packages: those are frozen at 2.x.
- Apps: `<PublishAot>true</PublishAot>`; publish early, because AOT warnings only show in the publish build.
- Docker: framework-dependent deploys need the `aspnet` runtime image; AOT builds need neither.

**Shape**

```csharp
public sealed class App
{
    public WebApplicationBuilder Builder { get; }
    public Router Router { get; } = new();
    internal JsonSerializerOptions Json { get; }

    public App(string[] args, params IJsonTypeInfoResolver[] json)
    {
        Builder = WebApplication.CreateSlimBuilder(args);
        Json = new()
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine([.. json, MerlinJson.Default]),
            NumberHandling = JsonNumberHandling.AllowReadingFromString   // for form binding
        };
    }

    public Task StartAsync()
    {
        var app = Builder.Build();
        Router.Lock();                                   // no Map after start
        app.Run(ctx => Adapter(ctx, app));
        return app.RunAsync();
    }

    private async Task Adapter(HttpContext ctx, WebApplication app)
    {
        var reply = Router.Match(ctx.Request.Method, ctx.Request.Path.Value ?? "/") switch
        {
            Matched m          => await Run(m, ctx),
            MethodNotAllowed n => Reply.MethodNotAllowed(n.Allowed),
            _                  => Reply.NotFound()
        };
        // write reply with Json (no body for HEAD); on Failure: log, and write an error reply if !ctx.Response.HasStarted
        // a Cancelled failure or an aborted request: no error log, no reply
    }
}
```

The app supplies its own source-generated context:

```csharp
[JsonSerializable(typeof(PostDto))]
[JsonSerializable(typeof(Post))]
internal partial class AppJson : JsonSerializerContext;

var app = new Merlin.App(args, AppJson.Default);
```

## Replies

A `Reply` is an immutable record holding a status code and headers; the base class sets them and subclasses only write the body, straight to the response stream.

- Status and headers change with `with`: `reply with { StatusCode = 201 }`.
- Headers are an `ImmutableDictionary`, set (not appended) onto the response, so nothing duplicates.
- No `byte[]` buffering: each reply streams.
- "Result" always means Wrap's type; Merlin's type is `Reply`, in `Merlin.Replies`.

```csharp
public abstract record class Reply
{
    public int StatusCode { get; init; } = StatusCodes.Status200OK;
    public ImmutableDictionary<string, string> Headers { get; init; }

    protected Reply(string contentType) =>
        Headers = ImmutableDictionary<string, string>.Empty
            .Add("Content-Type", contentType)
            .Add("Server", "Merlin")
            .Add("X-Clacks-Overhead", "GNU Terry Pratchett");

    // json is App.Json, passed in by the adapter, so a reply is a plain value that can be built anywhere
    public Task<Result<bool>> WriteAsync(HttpResponse response, JsonSerializerOptions json, CancellationToken ct)
    {
        SetHeaders(response);
        return R.TryAsync(async () => { await WriteBodyAsync(response.Body, json, ct); return true; });
    }

    protected virtual void SetHeaders(HttpResponse response)
    {
        response.StatusCode = StatusCode;
        foreach (var (key, value) in Headers) response.Headers[key] = value;
    }

    protected abstract Task WriteBodyAsync(Stream stream, JsonSerializerOptions json, CancellationToken ct);
}

public sealed record class TextReply(string? Content) : Reply("text/plain; charset=utf-8")
{
    protected override Task WriteBodyAsync(Stream s, JsonSerializerOptions _, CancellationToken ct) =>
        s.WriteAsync(Encoding.UTF8.GetBytes(Content ?? ""), ct).AsTask();
}

public sealed record class JsonReply<T>(T Content) : Reply("application/json")
{
    // an unregistered T throws NotSupportedException, which WriteAsync turns into a Failure (a logged 500)
    protected override Task WriteBodyAsync(Stream s, JsonSerializerOptions json, CancellationToken ct) =>
        JsonSerializer.SerializeAsync(s, Content, (JsonTypeInfo<T>)json.GetTypeInfo(typeof(T)), ct);
}
```

**Static helpers**, all returning `Result<Reply>` so every helper is a step in a Result chain: `Reply.Text(…)`, `Reply.Json(value)`, `Reply.Problem(problem)`, `Reply.NotFound()`, `Reply.MethodNotAllowed(allowed)`, `Reply.Error(failure)`. Use them with `from reply in Reply.Text(…) select reply` or `Bind`. `Text`, `Json` and `Problem` are done; the rest are stubs.

- **Problem details.** 404, 405 and error replies are RFC 9457 problems (`application/problem+json`), written from the `Problem` record. Its members fix their own JSON names with attributes, so the format is the same under any options. `MethodNotAllowed` also sets `Allow`, joining the methods with `", "` in the order given (`Router.Match` sorts them).
- **`HEAD`** has no reply of its own: the adapter writes the headers and leaves out the body.

**Still to add**

- An empty-body reply for 204.
- Later: `FileReply`, `RedirectReply`.

## Failures and logging

Expected outcomes are replies; a `Failure` means something went wrong. Every Failure is logged, and if the response hasn't started, it becomes an error reply.

- **Not found / method not allowed** are produced by the router as replies (404 / 405 with `Allow`), never Failures.
- **Default mapping:** `Reply.Error` takes the status from the failure's `FailureKind` (Wrap 2.0.0-beta.5): `Invalid` 400, `Unauthenticated` 401, `Forbidden` 403, `NotFound` 404, `Conflict` 409, `Unavailable` 503, `Timeout` 504, anything else 500. A 4xx problem's `detail` is the failure's message with its args filled in (binder messages are written for the client); a 5xx has no `detail`. Context and exceptions go to the log, never to the client. Apps could later override the mapping with a `Func<Failure, Reply>` on `App`.
- **Cancellation is not an error.** A `Cancelled` failure, or any failure once `ctx.RequestAborted` is set, is not logged as an error and gets no reply.
- **Exceptions** thrown outside `R.TryAsync` (e.g. inside a handler) are still open: see the open questions.
- **`result is Failure f`** works: Wrap v2 hooks into C# union pattern matching through `HasValue` / `Value` / `TryGetValue`.

**Logging extension.** It goes on `ILogger`, not `Failure`, so it works as a method group: `result.Audit(fFail: logger.Log)`.

```csharp
public static class LoggerExtensions
{
    public static void Log(this ILogger logger, Failure failure)
    {
        var level = failure.Level.ToMsLevel();
        if (!logger.IsEnabled(level)) return;

        using var _ = failure.Context is string ctx ? logger.BeginScope(ctx) : null;
#pragma warning disable CA2254 // template comes from the Failure, by design
        logger.Log(level, failure.Exception, failure.Message, failure.Args);
#pragma warning restore CA2254
    }

    // Wrap's LogLevel is bit flags (Warning = 8, Error = 16); Microsoft's is sequential (Warning = 3). Never cast.
    internal static LogLevel ToMsLevel(this Wrap.Logging.LogLevel level) => level switch
    {
        Wrap.Logging.LogLevel.Verbose     => LogLevel.Trace,
        Wrap.Logging.LogLevel.Debug       => LogLevel.Debug,
        Wrap.Logging.LogLevel.Information => LogLevel.Information,
        Wrap.Logging.LogLevel.Error       => LogLevel.Error,
        Wrap.Logging.LogLevel.Fatal       => LogLevel.Critical,
        _                                 => LogLevel.Warning
    };
}
```

Start with it as `internal` in Merlin. Later it moves to a small `Wrap.Extensions.Logging` package, so Wrap itself stays free of the `Microsoft.Extensions.Logging` dependency.

## Routing

Routes are registered fluently against a template, parsed once into segments, and stored in one trie per HTTP method. Matching walks the method's trie one path segment at a time.

**Fluent registration**

```csharp
app.Router
    .Map("/posts", name: "posts")
        .Get(ListPosts)
        .Post(CreatePost);

app.Router
    .Map("/posts/{id:int}", name: "post")
        .Get(GetPost)
        .Put(UpdatePost)
        .Delete(DeletePost);

app.Router.Map("/posts/{slug}").Get(GetPostBySlug);           // untyped = string
app.Router.Map("/archive/{year:int}/{draft:bool}").Get(Archive);
```

**Templates**

```csharp
public sealed record class Template(string Source, ImmutableArray<Segment> Segments)
{
    public static Template Parse(string source, ParamTypes types) => ...;   // throws on invalid templates
}

public closed record class Segment;                                          // closed: switches over it are exhaustive
public sealed record class Literal(string Value) : Segment;                 // posts
public sealed record class Param(string Name, ParamType Type) : Segment;    // {id:int}, {slug}
public sealed record class CatchAll(string Name) : Segment;                 // {path:*}
```

Every parameter has the same shape, `{name}` or `{name:type}`: the name comes first, and what follows the colon says what it matches. A catch-all is the special type `*` (`{path:*}`), allowed only as the last segment, and it captures the rest of the path as a string.

**Typed parameters.** Each parameter has a type, resolved when the route is registered. Every supported type becomes a parser delegate built from `IParsable<T>`: generic calls are resolved at compile time, so this is AOT-safe. Values are parsed once, while matching, and stored typed.

```csharp
public delegate bool TryParseValue(string input, [NotNullWhen(true)] out object? value);

public sealed record class ParamType(string Name, Type Type, int Priority, TryParseValue TryParse)
{
    public static ParamType Create<T>(string name, int priority) where T : IParsable<T> =>
        new(name, typeof(T), priority, (string s, out object? v) =>
        {
            if (T.TryParse(s, CultureInfo.InvariantCulture, out var t)) { v = t; return true; }
            v = null; return false;
        });
}

public sealed class ParamTypes
{
    private readonly Dictionary<string, ParamType> types = new(StringComparer.OrdinalIgnoreCase)
    {
        ["int"]      = ParamType.Create<int>("int", 10),
        ["long"]     = ParamType.Create<long>("long", 20),
        ["guid"]     = ParamType.Create<Guid>("guid", 10),
        ["bool"]     = ParamType.Create<bool>("bool", 10),
        ["dateonly"] = ParamType.Create<DateOnly>("dateonly", 10),
        ["string"]   = ParamType.Create<string>("string", 100),   // the default for {name}
    };

    public void Add<T>(string name, int priority = 50) where T : IParsable<T> =>
        types.Add(name, ParamType.Create<T>(name, priority));   // throws on duplicates

    internal ParamType Get(string name) =>
        types.TryGetValue(name, out var t) ? t : throw new InvalidOperationException($"Unknown route type '{name}'.");
}
```

`string` implements `IParsable<string>`, so untyped parameters need no special case. Custom types register the same way: `app.Router.Types.Add<PostId>("postid")` enables `{id:postid}`. Lower priority numbers are tried first. The name \* is reserved: it marks a catch-all.

**Trie and nodes.** A trie is a tree where each step down is one path segment; shared prefixes are stored once. One root per method means the walk only ever sees routes for that method.

&#91;embedded content: GET trie for /posts, /posts/{id}, /posts/{id}/comments, /about\]

Matching `/posts/42/comments` takes the literal `posts`, finds no literal `42` so takes the param child (capturing `id`), then the literal `comments`.

```csharp
internal sealed class Node
{
    public Dictionary<string, Node> Literals { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<(string Name, ParamType Type, Node Node)> Params { get; } = [];   // sorted by Type.Priority
    public (string Name, Handler Handler)? CatchAll { get; set; }
    public Handler? Handler { get; set; }                                       // a route ends here
}
```

**Router and builder**

```csharp
public delegate Task<Result<Reply>> Handler(Req req);

public sealed class Router
{
    private readonly Dictionary<string, Node> tries = [with(StringComparer.OrdinalIgnoreCase)];   // method -> root node
    private readonly Dictionary<string, Template> names = [with(StringComparer.Ordinal)];

    public ParamTypes Types { get; } = new();

    public RouteBuilder Map(string source, string? name = null) { ... }   // Template.Parse(source, Types), register name
    internal void Add(string method, Template template, Handler handler) { ... }   // called only by RouteBuilder
    public RouteMatch Match(string method, string path) { ... }
    internal void Lock() { ... }
}

public sealed class RouteBuilder(Router router, Template template)
{
    public RouteBuilder Get(Handler h) => On(HttpMethods.Get, h);                         // async handler
    public RouteBuilder Get(Func<Req, Result<Reply>> h) => Get(req => Task.FromResult(h(req)));   // sync handler
    // Post, Put, Patch, Delete: the same pair of overloads

    public RouteBuilder Map(string source, string? name = null) => router.Map(source, name);   // chain the next route

    private RouteBuilder On(string method, Handler h) { router.Add(method, template, h); return this; }
}
```

Registration flows one way: `Router.Map` parses the template (using the router's own `Types`) and returns a `RouteBuilder`; each `.Get`, `.Post` and so on calls `Router.Add`, which walks or creates nodes in that method's trie and puts the handler on the last one. A `Map` with no methods registers nothing.

```csharp
app.Router
    .Map("/")
        .Get(req => Reply.Text("Home"))
    .Map("/hello/{name}")
        .Get(req => from name in req.Route<string>("name")
                    from text in Reply.Text($"Hello, {name}")
                    select text);
```

**Matching**

1. Split the path on `/`, dropping empty segments (so `//posts/` matches `/posts`). Don't decode: Kestrel's `Request.Path` is already decoded except `%2F`, so decoding again would decode twice, and keeping `%2F` encoded stops `..%2F..%2Fsecret` from splitting a segment or escaping a catch-all.
2. In the method's trie, at each node try the literal child first, then each parameter child in priority order: a parameter child matches only if its `TryParse` succeeds, and the parsed value is what's captured. Then try the catch-all (capture the rest, at least one segment, as a string). Backtrack to the next candidate if a branch dead-ends.
3. Path consumed at a node with a handler → `Matched(handler, values)`, with the values keyed case-insensitively.
4. Otherwise try the same path in the other methods' tries (on the miss path only): any hit → `MethodNotAllowed(allowed)`, with `allowed` sorted; none → `NotFound`.
5. `HEAD` uses an explicit `HEAD` mapping if there is one, else falls back to the `GET` trie. `allowed` lists `HEAD` once whenever `GET` or `HEAD` is mapped; an explicit `HEAD` alone does not make `GET` allowed.

```csharp
public closed record class RouteMatch;
public sealed record class Matched(Handler Handler, ImmutableDictionary<string, object> Values) : RouteMatch;   // typed values
public sealed record class MethodNotAllowed(ImmutableArray<string> Allowed) : RouteMatch;
public sealed record class NotFound : RouteMatch;
```

**Rules**

- Precedence is structural: literal, then typed parameters by priority, then `string`, then catch-all. Registration order never changes behaviour, except to break a tie between sibling params whose types have the same priority: the param added first is tried first (`NodeParam.Order`).
- A catch-all needs at least one segment: `/files/{path:*}` doesn't match `/files`, and `/{path:*}` doesn't match `/`.
- A value that doesn't parse means the route doesn't match: `/posts/abc` against `{id:int}` is a 404, not a 400. A path that doesn't parse isn't a resource.
- Parameters of different types can be siblings at one node (`{id:int}` beside `{slug}`); the same type with different names at one node is a conflict.
- Registration errors throw at startup: unbalanced braces, an unknown type, duplicate param names, a catch-all that isn't last, a duplicate method on a template, a sibling conflict, a reused route name, `Map` after `Lock`.
- Literal matching is case-insensitive; captured values are parsed with the invariant culture.
- Route names are kept with their parsed `Template` for URL generation later: `router.Url("post", ("id", 42))` → `/posts/42`.

## Req and binders

`Req` is plain immutable data: what a handler is allowed to see. Binders are extension methods that each return a `Result` (or a `Maybe` when absence is normal), so they compose with Wrap's async LINQ.

```csharp
public sealed record class Req(
    HttpContext Http,                                  // escape hatch
    ImmutableDictionary<string, object> RouteValues,   // already parsed and typed by the router
    JsonSerializerOptions JsonOptions                  // App.Json
)
{
    public string Method => Http.Request.Method;
    public string Path => Http.Request.Path.Value ?? "/";
    public IQueryCollection Query => Http.Request.Query;
    public CancellationToken Cancellation => Http.RequestAborted;
}
```

**Route values** are already typed, so the binder only checks the type. A failure here means the handler and its template disagree (asking for `long` when the template says `int`): a programming error, not bad input. **Query values** are still strings, so they parse through `IParsable<T>` (static abstract, no reflection) and bad input is a client error.

```csharp
public static Result<T> Route<T>(this Req req, string name) =>
    req.RouteValues.TryGetValue(name, out var v) && v is T value
        ? value
        : R.Fail("Route value {Name} is not a {Type}.", name, typeof(T).Name).Knd(FailureKind.Unexpected);

// a repeated key uses the first value (v.ToString() would join them with commas)
public static Result<T> Query<T>(this Req req, string name) where T : IParsable<T> =>
    req.Query.TryGetValue(name, out var v) && T.TryParse(v[0], CultureInfo.InvariantCulture, out var x)
        ? x : R.Fail("Query value {Name} is missing or invalid.", name).Knd(FailureKind.Invalid);

public static Maybe<T> QueryOrNone<T>(this Req req, string name) where T : IParsable<T> => ...;
```

**JSON bodies** have one path: the type is looked up in `req.JsonOptions` (`App.Json`), so `req.Json<T>()`, `req.Form<T>()` and `Reply.Json(value)` all read and write a type the same way. There is no `JsonTypeInfo<T>` overload: an `info` taken straight from an app context carries that context's plain defaults, not `App.Json`'s. Looking a type up is AOT-safe because it only searches registered resolvers; a missing registration surfaces as an `Unexpected` Failure (a logged 500) at runtime. Bad input is `Invalid` (400). `R.TryAsync` rather than catching `JsonException` alone means a cancelled read becomes a `Cancelled` Failure.

```csharp
public static Task<Result<T>> Json<T>(this Req req)
{
    if (!req.Http.Request.HasJsonContentType()) return R.Fail("Request body is not JSON.").Knd(FailureKind.Invalid);
    // info = (JsonTypeInfo<T>)req.JsonOptions.GetTypeInfo(typeof(T)); NotSupportedException ->
    //     "Type {Type} is not registered for JSON." (Unexpected)
    // deserialise with info inside R.TryAsync:
    //     null (empty body or `null`) -> "Request body is empty." (Invalid)
    //     JsonException -> "Request body is not valid JSON at {Path}." (Invalid)
}
```

**Forms** are mapped onto JSON, shaped by the type's own metadata (`info.Properties`, with `info` from `req.JsonOptions`), then deserialised with the same `JsonTypeInfo<T>`. Naming policy, `[JsonPropertyName]`, init-only properties and constructors all behave as for JSON, and a field that is left out keeps the property's default.

```csharp
public static async Task<Result<T>> Form<T>(this Req req)
{
    if (!req.Http.Request.HasFormContentType) return R.Fail("Request body is not a form.").Knd(FailureKind.Invalid);
    var info = /* looked up in req.JsonOptions, as for JSON */;
    var form = await req.Http.Request.ReadFormAsync(req.Cancellation);

    var buffer = new ArrayBufferWriter<byte>();
    using (var w = new Utf8JsonWriter(buffer))
    {
        w.WriteStartObject();
        foreach (var p in info.Properties)
        {
            if (form.TryGetValue(p.Name, out var values))
            {
                w.WritePropertyName(p.Name);
                WriteValue(w, info.Options, p.PropertyType, values);
            }
        }
        w.WriteEndObject();
    }
    // deserialise buffer.WrittenSpan with info; JsonException -> "Form field {Name} is not valid." (Invalid),
    // with the name from the JSON path ($.views -> views)
}

// bool, ignoring case: "on"/"true"/"1" -> true, "off"/"false"/"0" -> false, anything else Invalid (checkbox);
// Enumerable kind -> array of strings; anything else -> a JSON string (numbers read via AllowReadingFromString).
// Never WriteRawValue user input.
```

**A handler, end to end**

```csharp
static Task<Result<Reply>> UpdatePost(Req req) =>
    from id   in req.Route<PostId>("id")
    from body in req.Json<PostDto>()
    from post in Posts.Update(id, body, req.Cancellation)
    from reply in Reply.Json(post)
    select reply;
```

Later binders follow the same pattern: `req.Header<T>`, `req.Cookie`, `req.Claim(...)`.

## Dependencies

Handlers get their dependencies by closure, resolved once at startup. `Req` has no service provider, so a handler's dependencies are visible in its signature and tests can pass fakes directly.

```csharp
static HandlerAsync GetPost(IPostRepo repo) =>
    async req => /* uses repo */;

app.Builder.Services.AddSingleton<IPostRepo, PostRepo>();
app.Router
    .Map("/posts/{id:int}")
        .Get(sp => GetPost(sp.GetRequiredService<IPostRepo>()));
```

`RouteBuilder` takes a `Handler` (sync), a `HandlerAsync` or a `HandlerFactory`, and stores the `RequestHandler` union in the trie. `Router.Lock(app.Services)` runs after `Builder.Build()` and before Kestrel starts. It calls each factory once, wraps sync handlers, and leaves only `HandlerAsync`s to match. A missing service fails `StartAsync`, not the first request. Factories can only resolve singletons: `Lock` gives them a provider that throws for a scoped or transient registration, so the mistake fails `StartAsync`. Per-request things such as a DB connection are injected as a singleton that creates them (`DbDataSource`, `IDbContextFactory<T>`, `Func<T>`) and opened inside the handler.

## Static files and later work

Static files come from Kestrel's middleware or from Caddy, not from Merlin. A hand-written file server has to get path traversal, MIME types, `ETag` / `304`, `Range` / `206` and `HEAD` right, and none of that is the learning goal.

- **In-app:** `app.UseStaticFiles()` before the router runs; unmatched requests fall through to Merlin.
- **Deployed:** let Caddy serve the files, so Merlin only sees dynamic requests.
- **Kestrel stays.** It handles HTTP parsing, TLS, HTTP/2 and 3, and the limits that stop slow-client and smuggling attacks. A hand-written HTTP engine could be a second adapter beside it someday, never a replacement on the internet.

**Later, in rough order**

- `router.Group("/api")` for prefixes.
- URL generation from route names.
- `FileReply` and `RedirectReply`.
- Middleware or hooks around handlers (auth, timing); the old `Lifecycle` stubs are deleted until this is designed.
- `Wrap.Extensions.Logging` as its own package.

## Open questions and decisions

Questions that came up while writing the tests, and the decisions settled then, are in `decisions.md`.

- [x] **Failure kinds.** Wrap 2.0.0-beta.5's `Failure` has a `FailureKind`, and `Reply.Error` maps it to a status (see Failures and logging).
- [x] **Error body format.** RFC 9457 problem details (`application/problem+json`). A 4xx includes the failure's message as `detail`; a 5xx doesn't.
- [ ] **Overlapping param types.** `42` parses as `int`, `long` and `string`. Reject overlapping numeric types at one node (suggested), or allow them with priority winning? Anything alongside `string` is allowed, as the deliberate fallback.
- [ ] **Default param types.** Is `int`, `long`, `guid`, `bool`, `dateonly`, `string` the right built-in set? Add `decimal`, `double`, `datetime`?
- [ ] **Compile-time typed handlers.** Handler and template agree only at runtime. A typed `Map`, e.g. `.Get<int>((req, id, ct) => …)`, would let the compiler check it, at the cost of more API. Revisit once the basics work.
- [x] **Handler signature.** The separate `CancellationToken` parameter is dropped: handlers take only `Req`, and use `req.Cancellation`.
- [ ] **`Req.Http` visibility.** Public escape hatch for now; make it internal once header, cookie and claim binders exist?
- [x] **Trailing slashes.** `/posts/` is the same as `/posts` (settled 2026-11-26).
- [x] **Catch-all with zero segments.** Does `/files/{path:*}` match `/files`, with `path` empty? No: a catch-all needs at least one segment, and `/{path:*}` does not match `/` (settled 2026-11-26, see `decisions.md`).
- [ ] **Route name scope.** Names unique per router, or per template plus method?
- [ ] **Registration errors.** Throw (current choice), or collect them and fail `StartAsync` with a list?
- [ ] **Optional parameters.** Support `{id:int?}`, or require two `Map` calls?
- [ ] **Form nesting.** Support `Address.Street`-style names, or flat DTOs only for now?
- [ ] **Unhandled exceptions.** Catch at the adapter and turn them into a logged 500 Failure, or leave them to ASP.NET? Today Kestrel logs them and sends a 500 with no body.

## Build order and test plan

Build from the inside out: pure pieces first, so each can be tested without a server. You write the code and settle each API; Claude writes the xUnit v3 tests against it.

| Step | You build | Tests cover |
| --- | --- | --- |
| 1 | Tidy-up: delete `HttpServer.cs` and the `Lifecycle` stubs; `App` builds and runs | Smoke test: app starts, responds |
| 2 | `Reply`, `TextReply`, `JsonReply<T>`, static helpers | Status, headers set once, body bytes, JSON shape, `with` doesn't share headers |
| 3 | `LoggerExtensions` + level mapping | Every Wrap level maps correctly; exception and args passed; disabled levels skipped |
| 4 | `ParamType` + `ParamTypes` | Each built-in type parses valid and rejects invalid input; invariant culture; custom `Add<T>`; duplicate names throw; unknown type throws |
| 5 | `Template.Parse` | Literals, typed and untyped params, catch-all; every invalid template throws |
| 6 | `Node` + `Router.Add` | Tries built per method; params sorted by priority; each duplicate/conflict case throws; `Lock` blocks `Map` |
| 7 | `Router.Match` | Literal beats param; typed beats `string`; failed parse falls through or 404s; backtracking; typed captures; no second decoding; empty and trailing segments; 404 vs 405 + `Allow`; `HEAD` falls back to `GET` |
| 8 | `Req` + route/query binders | Route type match and mismatch; query `IParsable` success/failure; `Maybe` for optional |
| 9 | JSON binder | Wrong content type, empty body, invalid JSON, unregistered type, success |
| 10 | Form binder | Strings, numbers from strings, checkbox on/absent, multi-select arrays, bad value |
| 11 | Adapter, with Failure mapping | Through `StartAsync` on Kestrel at a free loopback port (`App_Tests.TestHost`): replies written with `App.Json`; 404 / 405; `HEAD` without a body; Failure → logged + error reply by kind; nothing written after `HasStarted`; cancellation not logged as an error |
| 12 | End to end | Covered by step 11's tests, which run the whole pipeline on a real port |

**Testing approach**

- Steps 2 to 9 need no server: replies write to a `DefaultHttpContext` with a `MemoryStream` body; `Req` is built from a `DefaultHttpContext` too.
- Router tests are table-driven (`[Theory]` with `[InlineData]` rows of method, path, expected match), so new edge cases are one line each.
- Failures are checked with Wrap's `Result.Testing` helpers (`AssertMessage` and friends).
- After each step, run `dotnet publish` on AppBasic so AOT warnings surface while the change is small.
