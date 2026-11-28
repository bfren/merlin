// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using Merlin.Domain;

namespace Merlin.Routing;

public sealed partial class Router
{
	/// <summary>
	/// Resolve the handlers of the routes that end at a node and at every node below it.
	/// </summary>
	/// <param name="method">HTTP method of the trie, for error messages.</param>
	/// <param name="node">Node to start from.</param>
	/// <param name="sp">Root service provider, passed to handler factories.</param>
	/// <exception cref="InvalidOperationException">A handler factory failed.</exception>
	private static void Resolve(Method method, Node node, IServiceProvider sp)
	{
		node.Route = node.Route?.Resolve(method, sp);

		if (node.CatchAll is NodeCatchAll c)
		{
			node.CatchAll = c with { Route = c.Route.Resolve(method, sp) };
		}

		foreach (var child in node.Literals.Values)
		{
			Resolve(method, child, sp);
		}

		foreach (var p in node.Params)
		{
			Resolve(method, p.Node, sp);
		}
	}
}
