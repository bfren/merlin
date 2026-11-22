// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Collections.Generic;
using System.Threading.Tasks;
using Merlin.Replies;
using Microsoft.AspNetCore.Http;
using Wrap;

namespace Merlin.Routing;

public sealed class Router
{
	public delegate Task<Result<Reply>> ExecuteAsync(HttpContext context);

	public Dictionary<string, ExecuteAsync> Routes { get; } =
		[];

	public async Task<Result<Reply>> DispatchAsync(HttpContext context)
	{
		var request = context.Request;

		if (Routes.TryGetValue(request.Path, out var route))
		{
			return await route(context);
		}

		return R.Fail("Unable to find route.");
	}
}
