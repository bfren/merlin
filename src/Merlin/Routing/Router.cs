// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Net;
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

	public Task<HttpResult> DispatchAsync(HttpListenerRequest request)
	{
		Console.WriteLine("Requested: {0}", request.Url);
		return Task.FromResult<HttpResult>(new JsonResult(
			new { name = request.QueryString["hello"] }
		));
	}
}
