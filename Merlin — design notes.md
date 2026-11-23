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
        app.Run(ctx => Execute(app, ctx));
        return app.RunAsync();
    }

    private async Task Execute(WebApplication app, HttpContext ctx)
    {
        var reply = Router.Match(ctx.Request.Method, ctx.Request.Path.Value ?? "/") switch
        {
            Matched m          => await Run(m, ctx),
            MethodNotAllowed n => Reply.MethodNotAllowed(n.Allowed),
            _                  => Reply.NotFound()
        };
        // write reply; on Failure: log, and write an error reply if !ctx.Response.HasStarted
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

    public Task<Result<bool>> WriteAsync(HttpResponse response, CancellationToken ct)
    {
        SetHeaders(response);
        return R.TryAsync(async () => { await WriteBodyAsync(response.Body, ct); return true; });
    }

    protected virtual void SetHeaders(HttpResponse response)
    {
        response.StatusCode = StatusCode;
        foreach (var (key, value) in Headers) response.Headers[key] = value;
    }

    protected abstract Task WriteBodyAsync(Stream stream, CancellationToken ct);
}

public sealed record class TextReply(string? Content) : Reply("text/plain; charset=utf-8")
{
    protected override Task WriteBodyAsync(Stream s, CancellationToken ct) =>
        s.WriteAsync(Encoding.UTF8.GetBytes(Content ?? ""), ct).AsTask();
}

public sealed record class JsonReply<T>(T Content, JsonTypeInfo<T> Info) : Reply("application/json")
{
    protected override Task WriteBodyAsync(Stream s, CancellationToken ct) =>
        JsonSerializer.SerializeAsync(s, Content, Info, ct);
}
```

**Still to add**

- Static helpers returning `Reply`, so `select` needs no cast: `Reply.Text(…)`, `Reply.Json(value, info)`, `Reply.NotFound()`, `Reply.MethodNotAllowed(allowed)` (sets `Allow`), `Reply.Problem(failure)`.
- An empty-body reply for 204 and for `HEAD`.
- Later: `FileReply`, `RedirectReply`.

## Failures and logging

Expected outcomes are replies; a `Failure` means something went wrong. Every Failure is logged, and if the response hasn't started, it becomes an error reply.

- **Not found / method not allowed** are produced by the router as replies (404 / 405 with `Allow`), never Failures.
- **Default mapping:** Failure → 500 with a bland body. Details go to the log, never to the client. Apps can override it with a `Func<Failure, Reply>` on `App`.
- **Exceptions** thrown outside `R.TryAsync` (e.g. inside a handler) should be caught at the adapter and treated as a 500 Failure.
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

public abstract record class Segment;
public sealed record class Literal(string Value) : Segment;                 // posts
public sealed record class Param(string Name, ParamType Type) : Segment;    // {id:int}, {slug}
public sealed record class CatchAll(string Name) : Segment;                 // {*rest}
```

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

`string` implements `IParsable<string>`, so untyped parameters need no special case. Custom types register the same way: `app.Router.Types.Add<PostId>("postid")` enables `{id:postid}`. Lower priority numbers are tried first.

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
public delegate Task<Result<Reply>> Handler(Req req, CancellationToken ct);

public sealed class Router
{
    private readonly Dictionary<string, Node> roots = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Template> names = new(StringComparer.Ordinal);

    public ParamTypes Types { get; } = new();

    public RouteBuilder Map(string template, string? name = null) { ... }   // Template.Parse(template, Types), register name
    internal void Add(string method, Template template, Handler handler) { ... }   // throws on duplicates and conflicts
    public RouteMatch Match(string method, string path) { ... }
    internal void Lock() { ... }
}

public sealed class RouteBuilder(Router router, Template template)
{
    public RouteBuilder Get(Handler h)    => On(HttpMethods.Get, h);
    public RouteBuilder Post(Handler h)   => On(HttpMethods.Post, h);
    public RouteBuilder Put(Handler h)    => On(HttpMethods.Put, h);
    public RouteBuilder Patch(Handler h)  => On(HttpMethods.Patch, h);
    public RouteBuilder Delete(Handler h) => On(HttpMethods.Delete, h);
    public RouteBuilder On(string method, Handler h) { router.Add(method, template, h); return this; }
}
```

**Matching**

1. Split the path on `/`, dropping empty segments, and URL-decode each one.
2. In the method's trie, at each node try the literal child first, then each parameter child in priority order: a parameter child matches only if its `TryParse` succeeds, and the parsed value is what's captured. Then try the catch-all (capture the rest as a string). Backtrack to the next candidate if a branch dead-ends.
3. Path consumed at a node with a handler → `Matched(handler, values)`.
4. Otherwise try the same path in the other methods' tries (on the miss path only): any hit → `MethodNotAllowed(allowed)`, none → `NotFound`.
5. `HEAD` falls back to the `GET` trie.

```csharp
public abstract record class RouteMatch;
public sealed record class Matched(Handler Handler, ImmutableDictionary<string, object> Values) : RouteMatch;   // typed values
public sealed record class MethodNotAllowed(ImmutableArray<string> Allowed) : RouteMatch;
public sealed record class NotFound : RouteMatch;
```

**Rules**

- Precedence is structural: literal, then typed parameters by priority, then `string`, then catch-all. Registration order never changes behaviour.
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
    JsonSerializerOptions Json                         // App.Json
)
{
    public string Method => Http.Request.Method;
    public string Path => Http.Request.Path.Value ?? "/";
    public IQueryCollection Query => Http.Request.Query;
    public CancellationToken Aborted => Http.RequestAborted;
}
```

**Route values** are already typed, so the binder only checks the type. A failure here means the handler and its template disagree (asking for `long` when the template says `int`): a programming error, not bad input. **Query values** are still strings, so they parse through `IParsable<T>` (static abstract, no reflection) and bad input is a client error.

```csharp
public static Result<T> Route<T>(this Req req, string name) =>
    req.RouteValues.TryGetValue(name, out var v) && v is T value
        ? value
        : R.Fail("Route value {Name} is not a {Type}.", name, typeof(T).Name);

public static Result<T> Query<T>(this Req req, string name) where T : IParsable<T> =>
    req.Query.TryGetValue(name, out var v) && T.TryParse(v.ToString(), CultureInfo.InvariantCulture, out var x)
        ? x : R.Fail("Query value {Name} is missing or invalid.", name);

public static Result<Maybe<T>> OptionalQuery<T>(this Req req, string name) where T : IParsable<T> => ...;
```

**JSON bodies** go through `JsonTypeInfo<T>`. Resolving by type from `req.Json` is AOT-safe because it only searches registered resolvers; a missing registration surfaces as a Failure at runtime, so keep the explicit overload too.

```csharp
public static async Task<Result<T>> Json<T>(this Req req, JsonTypeInfo<T> info)
{
    if (!req.Http.Request.HasJsonContentType()) return R.Fail("Request body is not JSON.");
    try
    {
        var value = await JsonSerializer.DeserializeAsync(req.Http.Request.Body, info, req.Aborted);
        return value is null ? R.Fail("Request body is empty.") : R.Wrap(value);
    }
    catch (JsonException e) { return R.Fail("Invalid JSON at {Path}.", e.Path); }
}

public static Task<Result<T>> Json<T>(this Req req) =>
    /* (JsonTypeInfo<T>)req.Json.GetTypeInfo(typeof(T)); NotSupportedException -> Failure */ ...;
```

**Forms** are mapped onto JSON, shaped by the type's own metadata (`info.Properties`), then deserialised with the same `JsonTypeInfo<T>`. Naming policy, `[JsonPropertyName]`, init-only properties and constructors all behave as for JSON.

```csharp
public static async Task<Result<T>> Form<T>(this Req req, JsonTypeInfo<T> info)
{
    if (!req.Http.Request.HasFormContentType) return R.Fail("Request body is not a form.");
    var form = await req.Http.Request.ReadFormAsync(req.Aborted);

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
    // deserialise buffer.WrittenSpan with info; JsonException -> Failure
}

// bool: "on"/"true"/"1" -> true (checkbox); Enumerable kind -> array of strings;
// anything else -> a JSON string (numbers read via AllowReadingFromString). Never WriteRawValue user input.
```

**A handler, end to end**

```csharp
static Task<Result<Reply>> UpdatePost(Req req, CancellationToken ct) =>
    from id   in req.Route<PostId>("id")
    from body in req.Json(AppJson.Default.PostDto)
    from post in Posts.Update(id, body, ct)
    select Reply.Json(post, AppJson.Default.Post);
```

Later binders follow the same pattern: `req.Header<T>`, `req.Cookie`, `req.Claim(...)`.

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

The biggest open decision is how a `Failure` tells the adapter "client's fault (400)" from "server's fault (500)". Binders hit it first.

- [ ] **Failure kinds.** Add an optional kind or status slot to Wrap v2's `Failure` while it's in beta, or wrap it on Merlin's side (e.g. an `HttpFailure` carrying a status)? Typed routes move route values out of this question; query, JSON and form binders still raise it.
- [ ] **Error body format.** Plain text, or RFC 9457 problem details (`application/problem+json`)? Does a 400 include the binder's message?
- [ ] **Overlapping param types.** `42` parses as `int`, `long` and `string`. Reject overlapping numeric types at one node (suggested), or allow them with priority winning? Anything alongside `string` is allowed, as the deliberate fallback.
- [ ] **Default param types.** Is `int`, `long`, `guid`, `bool`, `dateonly`, `string` the right built-in set? Add `decimal`, `double`, `datetime`?
- [ ] **Compile-time typed handlers.** Handler and template agree only at runtime. A typed `Map`, e.g. `.Get<int>((req, id, ct) => …)`, would let the compiler check it, at the cost of more API. Revisit once the basics work.
- [ ] **Handler signature.** Keep `CancellationToken` as a separate parameter, or drop it since `req.Aborted` already carries it?
- [ ] **`Req.Http` visibility.** Public escape hatch for now; make it internal once header, cookie and claim binders exist?
- [ ] **Trailing slashes.** Is `/posts/` the same as `/posts`, a redirect, or a 404?
- [ ] **Catch-all with zero segments.** Does `/files/{*path}` match `/files`, with `path` empty? Should a catch-all be typeable?
- [ ] **Route name scope.** Names unique per router, or per template plus method?
- [ ] **Registration errors.** Throw (current choice), or collect them and fail `StartAsync` with a list?
- [ ] **Optional parameters.** Support `{id:int?}`, or require two `Map` calls?
- [ ] **Form nesting.** Support `Address.Street`-style names, or flat DTOs only for now?
- [ ] **Unhandled exceptions.** Catch at the adapter and turn them into a logged 500 Failure, or leave them to ASP.NET?

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
| 7 | `Router.Match` | Literal beats param; typed beats `string`; failed parse falls through or 404s; backtracking; typed captures; decoding; 404 vs 405 + `Allow`; `HEAD` falls back to `GET` |
| 8 | `Req` + route/query binders | Route type match and mismatch; query `IParsable` success/failure; `Maybe` for optional |
| 9 | JSON binder | Wrong content type, empty body, invalid JSON, unregistered type, success |
| 10 | Form binder | Strings, numbers from strings, checkbox on/absent, multi-select arrays, bad value |
| 11 | Adapter: `Execute` with Failure mapping | Failure → logged + error reply; nothing written after `HasStarted`; exceptions become 500 |
| 12 | End to end | In-memory requests through the whole pipeline via `TestServer` or a real port |

**Testing approach**

- Steps 2 to 9 need no server: replies write to a `DefaultHttpContext` with a `MemoryStream` body; `Req` is built from a `DefaultHttpContext` too.
- Router tests are table-driven (`[Theory]` with `[InlineData]` rows of method, path, expected match), so new edge cases are one line each.
- Failures are checked with Wrap's `Result.Testing` helpers (`AssertMessage` and friends).
- After each step, run `dotnet publish` on AppBasic so AOT warnings surface while the change is small.
