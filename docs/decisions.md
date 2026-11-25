# Decisions from writing the tests

Questions that came up while writing the unit tests in `tests/Tests.Merlin`. Some are already settled; the rest need Ben's call. Each item names the tests that depend on it, so they can be changed once a decision is made.

Broader open questions are listed at the end of `design-notes.md`; where an item here overlaps one of them, it says so.

## Settled

- **Wrap.Testing is used for assertions.** It was added to `Directory.Packages.props` and `tests/Directory.Build.props`. Tests use `AssertOk`, `AssertTrue`, `AssertFailure`, `AssertSome` and `AssertNone`.
- **Logging tests use a fake `ILogger`, not NSubstitute.** `ILogger.Log<TState>` is called with an internal state type that is awkward to match with NSubstitute. The fake (`Log_Tests.TestLogger`) records each entry's level, exception, message, structured values and open scopes.
- **JSON and form tests check only that a Failure comes back.** The failure messages aren't fixed yet (see below).

## Open: routing

| Question | What the tests assume now | Tests |
| --- | --- | --- |
| Trailing slash: is `/posts/` the same as `/posts`? (also in design notes) | Yes, it is ignored | `Match_Tests.Literals.Ignores_Trailing_Slash`, `Add_Tests.Duplicates` (`/posts` and `/POSTS/` count as the same route) |
| Catch-all with no segments: does `/files/{path:*}` match `/files`? (also in design notes) | No, a catch-all needs at least one segment | `Match_Tests.Catch_All.Requires_At_Least_One_Segment` |
| Root catch-all: does `/{path:*}` match `/`? | Not tested; follows from the item above (currently no) | none |
| Does HEAD fall back to GET? | Yes, as in the test plan in the design notes | `Match_Tests.Methods.Head_Falls_Back_To_Get` |
| Are route value names case-sensitive in `req.Route<T>`? | Not tested; depends on the comparer `Router.Match` gives the values dictionary | none |
| A request path of `//` | Not tested in `Match` (templates `/`, `""` and `//` all parse to no segments) | none |

## Open: replies

| Question | What the tests assume now | Tests |
| --- | --- | --- |
| Status codes for the helpers | `NotFound` 404, `MethodNotAllowed` 405, `Error` 500 | `Reply/NotFound_Tests`, `MethodNotAllowed_Tests`, `Error_Tests` |
| Format of the `Allow` header | `"GET, PUT"` (comma and space, in the order given) | `MethodNotAllowed_Tests` |
| Error body format (also in design notes) | Not checked; the body must only leave out the failure's message and context | `Error_Tests.Does_Not_Write_Failure_Details` |
| Cancellation in `Reply.WriteAsync`: should a cancelled token become a Failure (through `R.TryAsync`) or be let through? | Not tested | none |

## Open: app adapter

| Question | What the tests assume now | Tests |
| --- | --- | --- |
| How should tests reach the adapter? It is an inline lambda in `App.StartAsync`, and `App` keeps the built `WebApplication` private. Either pull out `internal Task Execute(HttpContext ctx, ILogger logger)` and test it with a `DefaultHttpContext`, or run end to end through `Microsoft.AspNetCore.TestHost`, which needs a workaround to reach the server and stop it | Not tested yet; waiting until the adapter writes replies | none |

## Open: binders

| Question | What the tests assume now | Tests |
| --- | --- | --- |
| Repeated query key, e.g. `?tag=a&tag=b` | `Query<int>` fails and `QueryOrNone<int>` gives None. `Query<string>` returns `"a,b"` (from `ToString()` on all the values); not tested, as it may not be wanted | `Query_Tests.Failures.Repeated_Key_Returns_Failure_For_Int`, `QueryOrNone_Tests` |
| Numbers in forms: does `Form` read numbers from strings itself, or rely on `JsonNumberHandling.AllowReadingFromString` in the options? | `Form` handles it itself; the test context leaves the option off. If it relies on the option, `Form(AppJson.Default.X)` won't read numbers unless the app's own context turns it on | `Form_Tests.Numbers.Binds_Number_From_String` |
| Failure messages for `Json` and `Form` (e.g. "Request body is not JSON.", "Request body is empty.", "Invalid JSON at {Path}.", "Request body is not a form.") | Only checked as a Failure; exact text and the JSON path to be asserted once fixed | `Json_Tests`, `Form_Tests` |
| Form: an array field that is left out | Not tested (`null` or an empty array?) | none |
| Form: checkbox values other than `on`, `true` and `1` (e.g. `off`, `false`, `0`) | Not tested; only those three are expected to be true | `Form_Tests.Checkboxes` |
| Cancellation while reading a JSON or form body | Not tested | none |
