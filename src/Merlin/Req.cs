// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Collections.Immutable;
using System.Text.Json;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace Merlin;

public sealed record class Req(
	HttpContext Http,
	ImmutableDictionary<string, object> RouteValues,
	JsonSerializerOptions JsonOptions
)
{
	public string Method =>
		Http.Request.Method;

	public string Path =>
		Http.Request.Path.Value ?? string.Empty;

	public IQueryCollection Query =>
		Http.Request.Query;

	public CancellationToken Cancellation =>
		Http.RequestAborted;
}
