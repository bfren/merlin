// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Collections.Generic;
using System.Linq;
using Merlin.Domain;
using Wrap;
using Wrap.Extensions;

namespace Merlin.Routing;

/// <summary>
/// The route table: one trie per HTTP method, built from route templates at startup
/// and walked for each request.
/// </summary>
public sealed class Router
{
	private bool locked;

	private readonly Dictionary<Method, Node> tries =
		[];

	private readonly Dictionary<string, Template> names =
		[with(StringComparer.Ordinal)];

	/// <summary>
	/// Param types available to route templates - add custom types before mapping routes that use them.
	/// </summary>
	public ParamTypes Types { get; } =
		new();

	/// <summary>
	/// Start mapping a route template - add handlers for each HTTP method with the returned builder.
	/// </summary>
	/// <param name="source">Route template, e.g. <c>/posts/{id:int}</c>.</param>
	/// <param name="name">[Optional] Unique route name, for generating URLs.</param>
	/// <returns>A builder to add handlers for each HTTP method.</returns>
	/// <exception cref="InvalidOperationException">The router is locked, the template is invalid, or the name is already in use.</exception>
	public RouteBuilder Map(string source, string? name = null)
	{
		// routes cannot be added once the application has started
		EnsureUnlocked();

		// parse the template
		var template = Template.Parse(source, Types);

		// template names must be unique
		if (name is not null && !names.TryAdd(name, template))
		{
			throw new InvalidOperationException($"Route name '{name}' is already in use.");
		}

		// return RouteBuilder
		return new(this, template);
	}

	/// <summary>
	/// Add a handler to the end of a template's path through the trie for an HTTP method,
	/// creating nodes as needed.
	/// </summary>
	/// <param name="method">HTTP method.</param>
	/// <param name="template">Parsed route template.</param>
	/// <param name="handler">Request handler.</param>
	/// <exception cref="InvalidOperationException">The router is locked, the route is already mapped, or a parameter conflicts with an existing one.</exception>
	internal void Add(Method method, Template template, Handler handler)
	{
		// routes cannot be added once the application has started
		EnsureUnlocked();

		// get existing node if a template with this method has already been added
		var node = tries.TryGetValue(method, out var n) ? n : tries[method] = new();

		// add the template segments to this method's node
		foreach (var segment in template.Segments)
		{
			// create node from segment values
			node = segment switch
			{
				Literal l =>
					node.Literals.TryGetValue(l.Value, out var child)
						? child
						: node.Literals[l.Value] = new(),

				Param p =>
					ParamChild(node, p, template.Source),

				CatchAll c =>
					SetCatchAll(node, c, handler, template.Source)
			};
		}

		// the last node reached is where this route ends (a catch-all sets its own handler)
		if (template.Segments is not [.., CatchAll])
		{
			if (node.Handler is not null)
			{
				throw new InvalidOperationException($"{method} {template.Source} is already mapped.");
			}

			node.Handler = handler;
		}
	}

	/// <summary>
	/// Throw if routes have been locked.
	/// </summary>
	/// <exception cref="InvalidOperationException">The router is locked.</exception>
	private void EnsureUnlocked()
	{
		if (locked)
		{
			throw new InvalidOperationException("Routes are locked - add all routes before the app starts.");
		}
	}

	/// <summary>
	/// Get the child node for a parameter, creating it if needed.
	/// </summary>
	/// <param name="node">Parent node.</param>
	/// <param name="p">Parameter segment.</param>
	/// <param name="source">Route template, for error messages.</param>
	/// <returns>The child node.</returns>
	/// <exception cref="InvalidOperationException">A parameter of the same type but a different name already exists here.</exception>
	private static Node ParamChild(Node node, Param p, string source)
	{
		// if a Node exists of this type and with this name, return it
		var existing = node.Params.FindIndex(x => x.Type.Name == p.Type.Name);
		if (existing >= 0)
		{
			var (name, _, _, child) = node.Params[existing];
			return name == p.Name
				? child
				: throw new InvalidOperationException($"'{source}': {{{p.Name}}} conflicts with {{{name}}}.");
		}

		// create a new Node, sorted by ParamType Priority,
		// or the order in which Nodes to solve a tie
		var created = new Node();
		node.Params.Add(new(p.Name, p.Type, node.Params.Count, created));
		node.Params.Sort((a, b) => a.Type.Priority == b.Type.Priority
			? a.Order.CompareTo(b.Order)
			: a.Type.Priority.CompareTo(b.Type.Priority)
		);
		return created;
	}

	/// <summary>
	/// Set the catch-all route on a node.
	/// </summary>
	/// <param name="node">Node.</param>
	/// <param name="c">Catch-all segment.</param>
	/// <param name="handler">Request handler.</param>
	/// <param name="source">Route template, for error messages.</param>
	/// <returns>The same node (a catch-all is always the last segment).</returns>
	/// <exception cref="InvalidOperationException">A catch-all is already mapped here.</exception>
	private static Node SetCatchAll(Node node, CatchAll c, Handler handler, string source)
	{
		if (node.CatchAll is not null)
		{
			throw new InvalidOperationException($"'{source}': catch-all already mapped here.");
		}

		node.CatchAll = new(c.Name, handler);
		return node;
	}

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
	/// <returns>The route's handler and captured values, or null if no route matches.</returns>
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
	/// <returns>The route's handler and captured values, or null if no route matches.</returns>
	private static Maybe<Matched> Walk(Node node, string[] parts, int i, RouteValuesDictionary values)
	{
		// the whole path is used: a route ends here or this branch is a dead end
		if (i == parts.Length)
		{
			return node.Handler switch
			{
				Handler h =>
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
			NodeCatchAll c =>
				new Matched(c.Handler, values.Add(c.Name, string.Join('/', parts[i..]))),

			_ =>
				M.None
		};
	}

	/// <summary>
	/// Prevent any more routes being added.
	/// </summary>
	internal void Lock() =>
		locked = true;
}
