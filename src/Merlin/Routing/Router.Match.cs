// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Linq;
using Merlin.Domain;
using Wrap;
using Wrap.Extensions;

namespace Merlin.Routing;

public sealed partial class Router
{
	/// <summary>
	/// Match an HTTP method and path against the routes.
	/// </summary>
	/// <param name="method">HTTP method.</param>
	/// <param name="path">Request path.</param>
	/// <returns>The match result.</returns>
	public RouteMatch Match(Method method, string path)
	{
		// split like templates, but don't decode (Kestrel already has, except %2F)
		var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		// HEAD falls back to GET, unless HEAD is mapped explicitly
		var found = Find(method, parts).Match(
			fNone: () => method == Method.Head ? Find(Method.Get, parts) : M.None,
			fSome: M.Wrap
		);

		if (found is Some<Matched>(var (handler, values)))
		{
			return new Matched(handler, values);
		}

		// no route for this method: which methods does the path match?
		var allowed = tries.Keys.Where(m => Find(m, parts) is Some<Matched>).ToList();
		if (allowed.Count == 0)
		{
			return new NotFound();
		}

		// a route with GET answers HEAD too
		if (allowed.Contains(Method.Get))
		{
			allowed.Add(Method.Head);
		}

		// sorted so the Allow header is the same on every request
		return new MethodNotAllowed([.. allowed.Distinct().Order()]);
	}

	/// <summary>
	/// Find the route for a path in an HTTP method's trie.
	/// </summary>
	/// <param name="method">HTTP method.</param>
	/// <param name="parts">Request path segments.</param>
	/// <returns>The route's handler and captured values, or none if no route matches.</returns>
	private Maybe<Matched> Find(Method method, string[] parts) =>
		tries.TryGetValue(method, out var root) ? Walk(root, parts, 0, RouteValuesDictionary.Empty) : M.None;

	/// <summary>
	/// Walk the trie from a node, trying literal, then params in priority order, then catch-all,
	/// and backtracking when a branch dead-ends.
	/// </summary>
	/// <param name="node">Current node.</param>
	/// <param name="parts">Request path segments.</param>
	/// <param name="i">Index of the segment to match at this node.</param>
	/// <param name="values">Values captured so far (immutable, so backtracking discards a branch's values for free).</param>
	/// <returns>The route's handler and captured values, or none if no route matches.</returns>
	private static Maybe<Matched> Walk(Node node, string[] parts, int i, RouteValuesDictionary values)
	{
		// the whole path is used: a route ends here or this branch is a dead end
		if (i == parts.Length)
		{
			return node.Route switch
			{
				{ Handler: HandlerAsync h } =>
					new Matched(h, values),

				_ =>
					M.None
			};
		}

		var part = parts[i];

		if (node.Literals.TryGetValue(part, out var literal) && Walk(literal, parts, i + 1, values) is Some<Matched> l)
		{
			return l;
		}

		foreach (var (name, type, _, child) in node.Params)
		{
			if (!type.TryParse(part, out var value))
			{
				continue;
			}

			if (Walk(child, parts, i + 1, values.Add(name, value)) is Some<Matched> p)
			{
				return p;
			}
		}

		// at least one segment is left here, which a catch-all needs
		return node.CatchAll switch
		{
			NodeCatchAll { Route.Handler: HandlerAsync h } c =>
				new Matched(h, values.Add(c.Name, string.Join('/', parts[i..]))),

			_ =>
				M.None
		};
	}
}
