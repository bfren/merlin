// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Threading.Tasks;
using Merlin.Domain;

namespace Merlin.Routing;

/// <summary>
/// A route that ends at a <see cref="Node"/>: its handler and the template it came from.
/// </summary>
/// <param name="Source">Route template, for error messages.</param>
/// <param name="Handler">Handler as mapped, replaced by its resolved <see cref="HandlerAsync"/> when the router is locked.</param>
internal readonly record struct NodeRoute(string Source, RequestHandler Handler)
{
	/// <summary>
	/// Resolve this route's handler to the <see cref="HandlerAsync"/> used for requests.
	/// </summary>
	/// <param name="method">HTTP method, for error messages.</param>
	/// <param name="sp">Root service provider, passed to a handler factory.</param>
	/// <returns>This route with its resolved handler.</returns>
	/// <exception cref="InvalidOperationException">The handler factory threw or returned null; the original exception is the inner exception.</exception>
	public NodeRoute Resolve(Method method, IServiceProvider sp) =>
		this with { Handler = ToAsync(method, sp) };

	/// <summary>
	/// Turn a handler as mapped into the asynchronous handler used for requests: wrap a synchronous handler
	/// in a completed task, keep an asynchronous one, or call a factory.
	/// </summary>
	/// <param name="method">HTTP method, for error messages.</param>
	/// <param name="sp">Root service provider, passed to a handler factory.</param>
	/// <returns>The asynchronous handler.</returns>
	/// <exception cref="InvalidOperationException">The handler factory threw or returned null; the original exception is the inner exception.</exception>
	private HandlerAsync ToAsync(Method method, IServiceProvider sp)
	{
		try
		{
			return Handler switch
			{
				Handler h =>
					req => Task.FromResult(h(req)),

				HandlerAsync h =>
					h,

				HandlerFactory f =>
					f(sp) switch
					{
						HandlerAsync h =>
							h,

						_ =>
							throw new InvalidOperationException("The handler factory did not return a handler.")
					}
			};
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"{method} {Source}: the handler factory failed.", ex);
		}
	}
}

