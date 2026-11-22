// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;
using Merlin.Result;

namespace Merlin.Routing;

public sealed class Router
{
	//public static async Task<HttpResult> DispatchAsync(HttpListenerRequest request, IPrincipal? user)
	//{
	//	Console.WriteLine("Requested: {0}", request.Url);
	//	return new TextResult(
	//		string.Format(CultureInfo.InvariantCulture, "Hi, {0}!", request.QueryString["hello"])
	//	);
	//}

	public async Task<HttpResult> DispatchAsync(HttpListenerRequest request, IPrincipal? user)
	{
		var query = request.QueryString;
		JsonSerializer.Serialize(query.AllKeys.ToDictionary(x => x, x => query[x]));

		Console.WriteLine("Requested: {0}", request.Url);
		return new JsonResult(
			new { name = request.QueryString["hello"] }
		);
	}
}
