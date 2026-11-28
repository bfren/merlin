// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using Merlin.Domain;

namespace Merlin.Routing;

/// <summary>
/// Adds handlers for HTTP methods to a route template - returned by <see cref="Router.Map"/>.
/// </summary>
/// <remarks>
/// Each method has three overloads: a synchronous <see cref="Handler"/>, an asynchronous <see cref="HandlerAsync"/>,
/// and a <see cref="HandlerFactory"/>, which is called once with the app's services when the router is locked.
/// Every method returns this builder, and <see cref="Map"/> starts the next route, so routes can be chained.
/// </remarks>
/// <param name="router">Router the handlers are added to.</param>
/// <param name="template">Parsed route template.</param>
public sealed class RouteBuilder(Router router, Template template)
{
	/// <summary>
	/// Handle GET requests to this route with a synchronous handler.
	/// </summary>
	/// <param name="h">Synchronous request handler.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Get(Handler h) =>
		On(Method.Get, h);

	/// <summary>
	/// Handle GET requests to this route with an asynchronous handler.
	/// </summary>
	/// <param name="h">Asynchronous request handler.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Get(HandlerAsync h) =>
		On(Method.Get, h);

	/// <summary>
	/// Handle GET requests to this route with a handler factory.
	/// </summary>
	/// <param name="h">Request handler factory.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Get(HandlerFactory h) =>
		On(Method.Get, h);

	/// <summary>
	/// Handle HEAD requests to this route with a synchronous handler.
	/// </summary>
	/// <param name="h">Synchronous request handler.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Head(Handler h) =>
		On(Method.Head, h);

	/// <summary>
	/// Handle HEAD requests to this route with an asynchronous handler.
	/// </summary>
	/// <param name="h">Asynchronous request handler.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Head(HandlerAsync h) =>
		On(Method.Head, h);

	/// <summary>
	/// Handle HEAD requests to this route with a handler factory.
	/// </summary>
	/// <param name="h">Request handler factory.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Head(HandlerFactory h) =>
		On(Method.Head, h);

	/// <summary>
	/// Handle POST requests to this route with a synchronous handler.
	/// </summary>
	/// <param name="h">Synchronous request handler.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Post(Handler h) =>
		On(Method.Post, h);

	/// <summary>
	/// Handle POST requests to this route with an asynchronous handler.
	/// </summary>
	/// <param name="h">Asynchronous request handler.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Post(HandlerAsync h) =>
		On(Method.Post, h);

	/// <summary>
	/// Handle POST requests to this route with a handler factory.
	/// </summary>
	/// <param name="h">Request handler factory.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Post(HandlerFactory h) =>
		On(Method.Post, h);

	/// <summary>
	/// Handle PUT requests to this route with a synchronous handler.
	/// </summary>
	/// <param name="h">Synchronous request handler.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Put(Handler h) =>
		On(Method.Put, h);

	/// <summary>
	/// Handle PUT requests to this route with an asynchronous handler.
	/// </summary>
	/// <param name="h">Asynchronous request handler.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Put(HandlerAsync h) =>
		On(Method.Put, h);

	/// <summary>
	/// Handle PUT requests to this route with a handler factory.
	/// </summary>
	/// <param name="h">Request handler factory.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Put(HandlerFactory h) =>
		On(Method.Put, h);

	/// <summary>
	/// Handle PATCH requests to this route with a synchronous handler.
	/// </summary>
	/// <param name="h">Synchronous request handler.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Patch(Handler h) =>
		On(Method.Patch, h);

	/// <summary>
	/// Handle PATCH requests to this route with an asynchronous handler.
	/// </summary>
	/// <param name="h">Asynchronous request handler.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Patch(HandlerAsync h) =>
		On(Method.Patch, h);

	/// <summary>
	/// Handle PATCH requests to this route with a handler factory.
	/// </summary>
	/// <param name="h">Request handler factory.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Patch(HandlerFactory h) =>
		On(Method.Patch, h);

	/// <summary>
	/// Handle DELETE requests to this route with a synchronous handler.
	/// </summary>
	/// <param name="h">Synchronous request handler.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Delete(Handler h) =>
		On(Method.Delete, h);

	/// <summary>
	/// Handle DELETE requests to this route with an asynchronous handler.
	/// </summary>
	/// <param name="h">Asynchronous request handler.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Delete(HandlerAsync h) =>
		On(Method.Delete, h);

	/// <summary>
	/// Handle DELETE requests to this route with a handler factory.
	/// </summary>
	/// <param name="h">Request handler factory.</param>
	/// <returns>This builder, to add more methods.</returns>
	public RouteBuilder Delete(HandlerFactory h) =>
		On(Method.Delete, h);

	/// <summary>
	/// Handle requests to this route with the given HTTP method.
	/// </summary>
	/// <param name="method">HTTP method.</param>
	/// <param name="h">Request handler.</param>
	/// <returns>This builder, to add more methods.</returns>
	private RouteBuilder On(Method method, RequestHandler h)
	{
		router.Add(method, template, h);
		return this;
	}

	/// <summary>
	/// Start mapping another route template, so routes can be chained.
	/// </summary>
	/// <param name="source">Route template, e.g. <c>/posts/{id:int}</c>.</param>
	/// <param name="name">[Optional] Unique route name, for generating URLs.</param>
	/// <returns>A builder for the new route template.</returns>
	/// <exception cref="InvalidOperationException">The router is locked, the template is invalid, or the name is already in use.</exception>
	public RouteBuilder Map(string source, string? name = null) =>
		router.Map(source, name);
}
