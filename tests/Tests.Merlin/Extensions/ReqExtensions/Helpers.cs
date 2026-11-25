// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Wrap;

namespace Merlin.Extensions.ReqExtensions_Tests;

internal static class Helpers
{
	/// <summary>
	/// Build a request with the given route values and query string (e.g. <c>?a=1</c>).
	/// </summary>
	public static Req NewReq(ImmutableDictionary<string, object>? routeValues = null, string query = "")
	{
		var http = new DefaultHttpContext();
		http.Request.QueryString = new(query);
		return new(http, routeValues ?? ImmutableDictionary<string, object>.Empty, new JsonSerializerOptions());
	}

	/// <summary>
	/// The context a binder's failure should have.
	/// </summary>
	public static string? ContextOf(string function) =>
		R.Fail(string.Empty).Ctx(nameof(Merlin.Extensions.ReqExtensions), function).Context;
}
