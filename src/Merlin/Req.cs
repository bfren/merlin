// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text.Json;
using System.Threading;
using Merlin.Domain;
using Microsoft.AspNetCore.Http;

namespace Merlin;

/// <summary>
/// What a handler can see of the current request: the route values captured by the router,
/// the query string, and the JSON options binders use.
/// </summary>
/// <param name="Http">The current HTTP context - an escape hatch for anything binders don't cover yet.</param>
/// <param name="RouteValues">Typed values captured from the path when the route matched, keyed by parameter name.</param>
/// <param name="JsonOptions">JSON options used by the JSON and form binders.</param>
public sealed record class Req(
	HttpContext Http,
	RouteValuesDictionary RouteValues,
	JsonSerializerOptions JsonOptions
)
{
	/// <summary>
	/// HTTP method.
	/// </summary>
	public string Method =>
		Http.Request.Method;

	/// <summary>
	/// Request path ("/" if empty).
	/// </summary>
	public string Path =>
		Http.Request.Path.Value ?? "/";

	/// <summary>
	/// Query string values.
	/// </summary>
	public IQueryCollection Query =>
		Http.Request.Query;

	/// <summary>
	/// Cancelled when the client disconnects.
	/// </summary>
	public CancellationToken Cancellation =>
		Http.RequestAborted;
}
