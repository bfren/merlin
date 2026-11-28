// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text.Json;
using System.Threading;
using Merlin.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Merlin;

/// <summary>
/// What a handler can see of the current request: the route values captured by the router,
/// the query string, and the JSON options binders use.
/// </summary>
public sealed record class Req
{
	internal HttpContext Http =>
		field;

	/// <summary>
	/// JSON options used by the JSON and form binders.
	/// </summary>
	public JsonSerializerOptions JsonOptions { get; }

	/// <summary>
	/// Logger for the application.
	/// </summary>
	public ILogger Logger { get; }

	/// <summary>
	/// HTTP method.
	/// </summary>
	public Method Method =>
		Method.Wrap(Http.Request.Method);

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
	/// Typed values captured from the path when the route matched, keyed by parameter name.
	/// </summary>
	public RouteValuesDictionary RouteValues { get; }

	/// <summary>
	/// Cancelled when the client disconnects.
	/// </summary>
	public CancellationToken Cancellation =>
		Http.RequestAborted;

	/// <summary>
	/// Create a request from the HTTP context, a logger, the captured route values and the JSON options.
	/// </summary>
	/// <param name="http">HTTP context for the current request.</param>
	/// <param name="logger">Logger for the application.</param>
	/// <param name="values">Route values captured by the router.</param>
	/// <param name="json">JSON options used by binders.</param>
	public Req(HttpContext http, ILogger logger, RouteValuesDictionary values, JsonSerializerOptions json) =>
		(Http, Logger, RouteValues, JsonOptions) = (http, logger, values, json);
}
