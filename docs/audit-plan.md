# Audit plan (2026-11-28)

Findings from a whole-repo audit of `src/Merlin` and `apps/AppBasic`, kept here to work through later. The load assumed was a web server's: many requests at once, against routes set up at startup that never change after that. Numbers match the original report. Tick each one off and record the decision in `decisions.md` when it's done.

Suggested order: 1, 2, 3, then the rest.

## Must fix

- [x] **1. Every request gets an empty 200** (done 2026-11-28) (`src/Merlin/App.cs`, `Adapter`)
  - The adapter works out the reply and then drops it, so `/nope` returns 200 with an empty body (seen by running AppBasic). 404s, 405s, error replies and the HEAD rule all depend on this.
  - Fix: write the reply with `reply.WriteAsync(ctx.Response, Json, ctx.RequestAborted)`. For a Failure, write `Reply.Error(f)` unless the response has started or the request was cancelled. For HEAD, leave out the body.
  - Tests: the failing `App/StartAsync_Tests` already describe the expected behaviour.

- [ ] **2. Request paths are trimmed, which can change a value** (`src/Merlin/Routing/Router.Match.cs:23`)
  - `Match` splits with `StringSplitOptions.TrimEntries`, and Kestrel has already decoded `%20`. So `/files/notes%20/a.txt` gives the catch-all `notes/a.txt` (a different file), `/hello/ben%20` gives `ben`, and `/posts/%20` matches `/posts`.
  - Fix: use only `RemoveEmptyEntries` for request paths, and keep `TrimEntries` for templates.
  - Tests: add `Match_Tests` cases for a param and a catch-all with a trailing space.

- [ ] **3. The sample's POST route can never work** (`apps/AppBasic/Program.cs`, `Reply.Json(new { Hello = name })`)
  - Anonymous types can't be in a source-generated JSON context, so writing the reply throws `NotSupportedException` and returns a 500. You'll only see it once 1 is fixed.
  - Fix: use a record such as `record Hello(string Name)`, list it in an `AppJson` context, and create the app with `new App(args, AppJson.Default)`.

## Should fix

- [ ] **4. A handler that throws skips Merlin's error handling** (`src/Merlin/App.cs`, `await m.Handler(...)`)
  - The exception goes past the adapter: Kestrel logs it and sends a 500 with no body, so there's no problem reply and no `ILogger.Log(Failure)`. This is still open in `decisions.md`.
  - Fix: run the handler with `R.TryAsync`, flattened to one `Result<Reply>`, so an exception becomes a Failure with a kind.
  - Tests: a handler that throws gets a logged 500 problem reply.

- [ ] **5. The `JsonTypeInfo` overloads go against a settled decision** (`ReqExtensions.Json.cs`, `ReqExtensions.Form.cs`)
  - `req.Json(info)` reads `FirstName` while `req.Json<T>()` reads `firstName`, so the same type binds differently on two routes. `decisions.md` says these overloads should go.
  - Fix: make `Json(Req, JsonTypeInfo<T>)` private, give `Form<T>()` no `info` parameter, and update the `With_Info` and `Form_Tests` call sites.

- [ ] **6. `HtmlReply` isn't used anywhere** (`src/Merlin/Replies/HtmlReply.cs`)
  - It has no `Reply.Html` helper and no tests, and it's a copy of `TextReply` with a different content type.
  - Fix: delete it until it's needed, or add `Reply.Html(string)` and one test.

## Nice to have

- [ ] **7. `Query` and `QueryOrNone` repeat the same parsing** (`ReqExtensions.Query.cs`)
  - Fix: one private `TryQuery<T>` that both use, or write `QueryOrNone` from `Query`. That saves about 6 lines.

- [ ] **8. An empty chunked body gets the wrong message** (`ReqExtensions.Json.cs`, the `ContentLength == 0` check)
  - With no `Content-Length`, an empty body returns "Request body is not valid JSON at $." instead of "Request body is empty.". It's a 400 either way.
  - Fix: map a `JsonException` at `$` with no bytes read to `RequestIsEmpty()`, or record the difference in `decisions.md`.

- [ ] **9. Factories can get around the singleton check** (`src/Merlin/Routing/Router.cs`, `SingletonProvider`)
  - `sp.GetRequiredService<IServiceProvider>()` passes through and returns the root provider. Also, a new provider is created for every trie.
  - Fix: return `this` for `typeof(IServiceProvider)`, and create the provider once before the loop in `Lock`.

## Not checked in the audit

- Whether the tests actually assert what their names say (only coverage was checked).
- `docs/`.
- Kestrel's own `Server` header next to Merlin's.
- A real AOT-published run (only the publish warnings were checked).
- Worst-case backtracking in `Walk` with many sibling param types (no realistic slow route set was found).
