// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Microsoft.AspNetCore.Http;

namespace Merlin.Routing;

/// <summary>
/// Adds handlers for HTTP methods to a route template - returned by <see cref="Router.Map"/>.
/// </summary>
/// <param name="router">Router the handlers are added to.</param>
/// <param name="template">Parsed route template.</param>
public sealed class RouteBuilder(Router router, Template template)
{
	/// <summary>
	/// Handle GET requests to this route.
	/// </summary>
	/// <param name="h">Request handler.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Get(Handler h) =>
		On(HttpMethods.Get, h);

	/// <summary>
	/// Handle POST requests to this route.
	/// </summary>
	/// <param name="h">Request handler.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Post(Handler h) =>
		On(HttpMethods.Post, h);

	/// <summary>
	/// Handle PUT requests to this route.
	/// </summary>
	/// <param name="h">Request handler.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Put(Handler h) =>
		On(HttpMethods.Put, h);

	/// <summary>
	/// Handle PATCH requests to this route.
	/// </summary>
	/// <param name="h">Request handler.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Patch(Handler h) =>
		On(HttpMethods.Patch, h);

	/// <summary>
	/// Handle DELETE requests to this route.
	/// </summary>
	/// <param name="h">Request handler.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Delete(Handler h) =>
		On(HttpMethods.Delete, h);

	/// <summary>
	/// Handle requests to this route with the given HTTP method.
	/// </summary>
	/// <param name="method">HTTP method.</param>
	/// <param name="h">Request handler.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder On(string method, Handler h)
	{
		router.Add(method, template, h);
		return this;
	}

	public RouteBuilder Map(string source, string? name = null) =>
		router.Map(source, name);
}
