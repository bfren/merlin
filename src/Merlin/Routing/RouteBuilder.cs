// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Microsoft.AspNetCore.Http;

namespace Merlin.Routing;

public sealed class RouteBuilder(Router router, Template template)
{
	public RouteBuilder Get(Handler h) =>
		On(HttpMethods.Get, h);

	public RouteBuilder Post(Handler h) =>
		On(HttpMethods.Post, h);

	public RouteBuilder Put(Handler h) =>
		On(HttpMethods.Put, h);

	public RouteBuilder Patch(Handler h) =>
		On(HttpMethods.Patch, h);

	public RouteBuilder Delete(Handler h) =>
		On(HttpMethods.Delete, h);

	public RouteBuilder On(string method, Handler h)
	{
		router.Add(method, template, h);
		return this;
	}
}
