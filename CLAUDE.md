# Merlin

Merlin is a small, AOT-safe, Hono-like web framework for .NET, built on Ben's Wrap library. A handler is a function from a request to `Task<Result<Reply>>`; Kestrel handles HTTP underneath, and Merlin owns routing, binding, replies and failure handling.

The full design, with code shapes and open decisions, is in `docs/design-notes.md`. Read it before making design suggestions. Where it differs from the code, the code is current.

## How we work

- **Ben writes the production code and settles the API. Claude writes the tests.** Don't change code in `src/` unless Ben asks; if a test exposes a bug, report it with the failing test rather than fixing the code.
- **Don't commit.** Ben's commits must be GPG-signed (branch protection), so he commits himself. Commit messages are short gerund phrases, e.g. "Adding route matching tests".
- Ben prefers verifying claims empirically: build and run the tests rather than reasoning about whether code works.
- Ben prefers functional, composable code over OOP patterns.

## Stack

- .NET 11 (`net11.0`), C# 15: `closed` record hierarchies, collection expression arguments (`[with(StringComparer.Ordinal)]`) and C# unions are all in use.
- Wrap 2.0.0-beta.4 (`Result<T>`, `Maybe<T>`, `Failure`, `R.Fail`, `R.TryAsync`, async LINQ via `Wrap.Linq`). `result is Failure f` works via union pattern matching.
- Tests: xUnit v3, NSubstitute, Rnd (`Rnd.Str`, `Rnd.Int`, ...), Microsoft.Testing.Platform runner (see `global.json`). Central package management in `Directory.Packages.props`.
- `GenerateDocumentationFile` is on in `src`: every public and internal member needs XML docs.

## Layout

- `src/Merlin/App.cs`: builds a slim `WebApplication`, locks the router, matches each request and runs its handler.
- `src/Merlin/Routing/`: `Router` (one trie per HTTP method, keyed in `tries`), `Node` (a point in a trie), `Template` + `Segment` (`Literal`, `Param`, `CatchAll`; a `closed` hierarchy), `ParamType`/`ParamTypes` (typed params via `IParsable<T>`), `RouteBuilder` (fluent `Map(...).Get(...).Post(...)`), `RouteMatch` (`Matched`, `MethodNotAllowed`, `NotFound`; a `closed` hierarchy).
- `src/Merlin/Replies/`: `Reply` (status, headers, `WriteAsync`), `TextReply`, `JsonReply<T>`. Static helpers on `Reply` all return `Result<Reply>` for consistency.
- `src/Merlin/Extensions/`: partial static extension classes, one method per file in a folder, e.g. `ReqExtensions/ReqExtensions.Route.cs`.
- `src/Merlin/Req.cs`: what a handler sees (HTTP context, typed route values, JSON options, cancellation).
- `apps/AppBasic`: a sample app showing the fluent API.
- `tests/Tests.Merlin`: unit tests.

## Settled decisions

- Route templates: `/posts/{id:int}` (typed), `/posts/{slug}` (untyped = `string`), `/files/{path:*}` (catch-all, last segment only). `*` is a reserved param type name.
- Matching: literal, then typed params by priority (lower first), then `string`, then catch-all, with backtracking; registration order never matters. A value that doesn't parse means no match (404), not a 400. Literals and methods are case-insensitive; values are URL-decoded.
- Registration errors (bad template, duplicate route or name, sibling param conflict, `Map` after start) throw at startup.
- Not found and method not allowed are replies from the router, not Failures. Failures are logged (`ILogger.Log(Failure)`) and become error replies.
- Handlers: one argument (`Req`); cancellation is `req.Cancellation`. `RouteBuilder` has sync overloads (`Func<Req, Result<Reply>>`) wrapped in `Task.FromResult`.

## Current state

Done: templates and parsing, typed param types, registration into the tries, fluent mapping, replies (partly), logging extension, route and query binders.

Not done yet (throw `NotImplementedException`): `Router.Match`, `Reply.Error`, `Reply.MethodNotAllowed`, `Reply.NotFound`. Also not started: writing the reply in `App`, the JSON and form binders, URL generation from route names.

Open questions are listed at the end of `docs/design-notes.md` (failure kinds for 400 vs 500, error body format, trailing slash and zero-segment catch-all behaviour, and others). A proposal to add `FailureKind` to Wrap's `Failure` is with the Wrap project.

## Test conventions

Follow the style of Ben's Wrap tests:

- One file per method under test: `tests/Tests.Merlin/<Folder>/<Type>/<Method>_Tests.cs`.
- Namespace `Merlin.<Folder>.<Type>_Tests`; class `<Method>_Tests`; nested classes group scenarios; method names describe the behaviour (`Literal_Beats_Param`).
- `// Arrange`, `// Act`, `// Assert` comments; `Rnd` for arbitrary values; `Substitute.For<Handler>()` for distinct handlers.
- Prefer testing through the public API. Internals are visible to `Tests.Merlin` when needed.
- File header: `// Merlin: Unit Tests` and `// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022`.

`tests/Tests.Merlin/Routing/Router/Match_Tests.cs` already exists (28 tests). It was written without being compiled or run, so start by building and running it.
