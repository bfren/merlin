# Decisions from writing the tests

Questions that came up while writing the unit tests in `tests/Tests.Merlin`. Some are already settled; the rest need Ben's call. Each item names the tests that depend on it, so they can be changed once a decision is made.

Broader open questions are listed at the end of `design-notes.md`; where an item here overlaps one of them, it says so.

## Settled

- **Wrap.Testing is used for assertions.** It was added to `Directory.Packages.props` and `tests/Directory.Build.props`. Tests use `AssertOk`, `AssertTrue`, `AssertFailure`, `AssertSome` and `AssertNone`.
- **Logging tests use a fake `ILogger`, not NSubstitute.** `ILogger.Log<TState>` is called with an internal state type that is awkward to match with NSubstitute. The fake (`Log_Tests.TestLogger`) records each entry's level, exception, message, structured values and open scopes.
- **JSON and form tests check only that a Failure comes back.** The failure messages aren't fixed yet (see below).

### Routing (settled 2026-11-26)

- **Trailing slashes are ignored.** `/posts/` matches `/posts`, as templates already treat them as the same route. Tests: `Match_Tests.Literals.Ignores_Trailing_Slash`, `Add_Tests.Duplicates`.
- **Route value names are case-insensitive.** `Match` builds the values dictionary with `StringComparer.OrdinalIgnoreCase`; templates already reject names that differ only in case. Tests: none yet.
- **Request paths are split like templates.** Empty parts are dropped, so `//posts` matches `/posts`. Tests: none yet.
- **Ties in param priority go to the param added first.** `NodeParam` carries an `Order`, and `Router.Add` sorts by priority, then by order. This is the one case where registration order matters; `CLAUDE.md` says so. Tests: `Match_Tests.Precedence.Equal_Priority_Tie_Goes_To_Param_Added_First`.
- **`Match` does not decode.** Kestrel's `Request.Path` is already decoded except `%2F` (checked against Kestrel on 2026-11-26), so `Match` matches the path as given. Decoding again would decode twice (`100%2525` would become `100%`). `%2F` stays encoded in every value, so it never splits a segment, and a catch-all can't be turned into `../../secret` by `..%2F..%2Fsecret`, which would get past Kestrel's own `..` handling. Tests: `Match_Tests.Params.Does_Not_Decode_Value`, `Encoded_Slash_Does_Not_Split_Segment`, `Catch_All.Keeps_Encoded_Slash`.
- **HEAD.** A route with GET answers HEAD automatically, by running the GET handler (the adapter must then leave out the body). An explicit HEAD mapping wins over the fallback, so a route can answer HEAD without doing the GET work (`RouteBuilder.Head`). A route without GET or HEAD gives 405 for HEAD, and an explicit HEAD alone does not make GET allowed. `Allow` lists HEAD whenever GET or HEAD is mapped, once. Tests: `Match_Tests.Methods` (`Head_Falls_Back_To_Get`, `Head_Falls_Back_To_Get_With_Params`, `Explicit_Head_Beats_Get_Fallback`, `Head_Without_Get_Returns_MethodNotAllowed`, `Head_Does_Not_Make_Get_Allowed`, `Returns_MethodNotAllowed_With_Allowed_Methods`, `Allowed_Methods_List_Head_Once_When_Mapped_Explicitly`, `Allowed_Methods_Only_Include_Methods_Matching_Path`); `RouteBuilder/Head_Tests` (`Adds_Async_Handler_For_HEAD`, `Adds_Sync_Handler_For_HEAD`, `Does_Not_Add_Handler_For_Other_Methods` (checks nothing is added for GET), `Returns_Same_Builder`, `Sync_Overload_Returns_Same_Builder`).

### Replies (settled 2026-11-26)

- **`Allow` joins the methods with a comma and a space, in the order given** (`GET, HEAD, PUT`). `Router.Match` sorts the methods it returns, so the header is the same on every request; the tries are a `Dictionary`, whose order isn't guaranteed. Tests: `Reply/MethodNotAllowed_Tests`; the `Match_Tests` compare `Allowed` after sorting, so they don't check that `Match` sorts.
- **A cancelled request is not an error.** A write to a cancelled token returns a Failure at level Error holding a `TaskCanceledException` (checked on 2026-11-26), so a client that disconnects would otherwise be logged as an error. The adapter checks `ctx.RequestAborted.IsCancellationRequested`: if it is set, the Failure is not logged as an error (Debug at most) and no error reply is written. This covers a cancelled write and a handler whose work is cancelled. Tests: none yet; they belong with the adapter tests.
- **`Problem` fixes its own JSON format, and `MerlinJson` is part of `App.Json`.** Each member has `[JsonPropertyName]` (`title`, `status`, `type`, `detail`, `instance`), and the optional ones have `[JsonIgnore(Condition = WhenWritingNull)]`, so the RFC 9457 format is the same whatever options are used (checked on 2026-11-26 under Web defaults, no naming policy and snake_case). `MerlinJson` has no options of its own and is added to `App.Json`'s resolver (`Combine([.. json, MerlinJson.Default])`). Tests: `Reply/Problem_Tests`, `App/Json_Tests`.
- **`Reply.Error` takes its status from the failure's `FailureKind`** (Wrap 2.0.0-beta.5). `Invalid` 400, `Unauthenticated` 401, `Forbidden` 403, `NotFound` 404, `Conflict` 409, `Unavailable` 503, `Timeout` 504; `Unexpected`, `Unspecified` and any custom kind 500. A Failure created from an exception gets its kind from it (`Timeout` for a `TimeoutException`, `Cancelled` for any other `OperationCanceledException`, `Unexpected` otherwise). `Cancelled` writes no reply: the adapter checks for it as well as `RequestAborted` (see the cancellation decision above). Whatever the kind, the body never includes the failure's message or context. Tests: `Reply/Error_Tests` (`Status.Maps_Kind_To_Status`, `Defaults_To_500`, `Custom_Kind_Is_500`, `Uses_Kind_Chosen_From_Exception`, `Writes_Status`, `Body.Does_Not_Write_Failure_Details`).

## Open: routing

| Question | What the tests assume now | Tests |
| --- | --- | --- |
| Catch-all with no segments: does `/files/{path:*}` match `/files`? (also in design notes) | No, a catch-all needs at least one segment | `Match_Tests.Catch_All.Requires_At_Least_One_Segment` |
| Root catch-all: does `/{path:*}` match `/`? | Not tested; follows from the item above (currently no) | none |

## Open: replies

| Question | What the tests assume now | Tests |
| --- | --- | --- |
| Status codes for the helpers | `NotFound` 404, `MethodNotAllowed` 405, `Error` 500 | `Reply/NotFound_Tests`, `MethodNotAllowed_Tests`, `Error_Tests` |
| Error body format (also in design notes). RFC 9457 problem details (`application/problem+json`): the `Problem` record, the internal `MerlinJson` context and `Reply.Problem` now exist. Still open: whether 404, 405 and 500 use it, any extra members, and what `detail` may contain (waits for `FailureKind`) | `Reply.Problem` sets the status from `Problem.Status` and the problem content type; members are written with RFC 9457 names, and unset ones are left out. `Error` must leave out the failure's message and context | `Reply/Problem_Tests`, `Error_Tests.Does_Not_Write_Failure_Details` |

## Open: app adapter

| Question | What the tests assume now | Tests |
| --- | --- | --- |
| How should tests reach the adapter? It is an inline lambda in `App.StartAsync`, and `App` keeps the built `WebApplication` private. Either pull out `internal Task Execute(HttpContext ctx, ILogger logger)` and test it with a `DefaultHttpContext`, or run end to end through `Microsoft.AspNetCore.TestHost`, which needs a workaround to reach the server and stop it | Not tested yet; waiting until the adapter writes replies | none |


## JSON options (settled 2026-11-26)

Checked on 2026-11-26: when a source-generated context is used as the resolver for other options (as `App.Json` does), those options apply and the context's own `[JsonSourceGenerationOptions]` are ignored. A camelCase, ignore-nulls context wrote `{"firstName":"Ben"}` through its own type info, but `{"FirstName":"Ben","Age":null}` through App-style options. So two routes to the same type can read and write it differently.

- **`App.Json` holds the only options.** camelCase names, case-insensitive reads and numbers read from strings (the same as `JsonSerializerDefaults.Web`). Nulls are written; add `DefaultIgnoreCondition = WhenWritingNull` to leave them out. App contexts and `MerlinJson` only list types, with no `[JsonSourceGenerationOptions]`. Tests: `App/Json_Tests`.
- **One path: all JSON goes through `App.Json`.** Type information is registered once, at startup, by the contexts passed to `new App(args, AppJson.Default)`. Binders and replies look types up there (`GetTypeInfo(typeof(T))`) instead of taking a `JsonTypeInfo<T>`, so `req.Json<T>()`, `req.Form<T>()` and `Reply.Json(value)` all use the same options. An unregistered type can only be found when the body is read or written (handlers are plain functions, so Merlin can't know their types at startup): the lookup throws `NotSupportedException`, which becomes a Failure (a logged 500). The `info` overloads are dropped, because an `info` taken straight from an app context carries that context's plain defaults (`FirstName`), not `App.Json`'s. Tests: `Json_Tests.Without_Info.*` stay; `Json_Tests.With_Info.*`, `Form_Tests` and `Reply/Json_Tests` change once the signatures do.
- **Replies get the options when they are written, not when they are created.** `App.Json` is not static (each `App` has its own resolvers), so the adapter passes it in: `reply.WriteAsync(ctx.Response, Json, ctx.RequestAborted)`, which passes it on to `WriteBodyAsync`. `JsonReply<T>` then holds only the value. A reply stays a plain value that can be built anywhere. Tests: the reply tests pass their own options once the signature changes.

## Open: writing replies

| Question | Recommended | Tests |
| --- | --- | --- |
| `WriteBodyAsync` is shared by every reply, but only `JsonReply` needs the JSON options | Pass the options to every reply and let the others ignore them (an unused parameter on an override is normal). If a second need appears (e.g. a `FileReply` wanting a file provider), replace the parameter with a small record such as `WriteContext(JsonSerializerOptions Json, ...)` | none yet |

## Binders (settled 2026-11-26)

- **Binder failures have a `FailureKind`.** Bad client input is `Invalid` (400): a query value that is missing or doesn't parse, the wrong content type, an empty body, invalid JSON, or a form field that can't be converted. `Invalid` is used for the wrong content type too, rather than 415, because Wrap has no kind for it and custom kinds map to 500. A `Route<T>` failure means the handler and its template disagree, which is a programming error, so it is `Unexpected` (500); so is an unregistered type in `Json<T>()`. Tests: `Route_Tests`, `Query_Tests`, `Json_Tests`, `Form_Tests` (`*_Has_Kind_*`).
- **Failure messages.** `Query` and `Route` keep their messages. `Json`: "Request body is not JSON.", "Request body is empty." (also for a body of `null`), "Request body is not valid JSON at {Path}." and "Type {Type} is not registered for JSON." `Form`: "Request body is not a form." and "Form field {Name} is not valid.", where the name comes from the JSON path (`$.views` gives `views`). Tests: `Json_Tests`, `Form_Tests`.
- **A repeated query key uses the first value**, as ASP.NET's own binding does: `?page=1&page=2` gives 1, and `?tag=a&tag=b` gives `"a"` for `Query<string>`. A `QueryAll<T>` returning every value can come later. Tests: `Query_Tests.Valid.Repeated_Key_Uses_First_Value`, `QueryOrNone_Tests.Repeated_Key_Uses_First_Value`.
- **A form field that is left out keeps the property's default**, as in JSON: `null` for an array unless the type sets one (e.g. `= []`), `false` for a checkbox. Tests: `Form_Tests.Arrays.Absent_Is_Default`, `Checkboxes.Absent_Is_False`.
- **Checkboxes:** `on`, `true` and `1` are true; `off`, `false` and `0` are false; case is ignored. Anything else is an `Invalid` failure rather than false. Tests: `Form_Tests.Checkboxes`.
- **Numbers in forms come from `App.Json`**, which reads numbers from strings (part of the one-path JSON decision). `Form_Tests.Numbers.Binds_Number_From_String` passes `App.Json`-like options once `Form` drops its `info`.
- **Cancellation while reading a body becomes a `Cancelled` Failure.** The binders use `R.TryAsync` rather than catching only `JsonException`, so Wrap gives the Failure the kind `Cancelled` and the adapter's cancellation rule applies (no error log, no reply). Tests: `Json_Tests.Failures.Cancelled_Read_Has_Kind_Cancelled`, `Form_Tests.Content_Types.Cancelled_Read_Has_Kind_Cancelled`.

