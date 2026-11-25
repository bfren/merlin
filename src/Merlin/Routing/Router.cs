// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Collections.Generic;

namespace Merlin.Routing;

/// <summary>
/// The route table: one trie per HTTP method, built from route templates at startup
/// and walked for each request.
/// </summary>
public sealed class Router
{
	private bool locked;

	private readonly Dictionary<string, Node> tries =
		[with(StringComparer.OrdinalIgnoreCase)];

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
	internal void Add(string method, Template template, Handler handler)
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
			throw new InvalidOperationException("Routes are locked - add new routes before the app starts.");
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
		var existing = node.Params.FindIndex(x => x.Type.Name == p.Type.Name);
		if (existing >= 0)
		{
			var (name, _, child) = node.Params[existing];
			return name == p.Name
				? child
				: throw new InvalidOperationException($"'{source}': {{{p.Name}}} conflicts with {{{name}}}.");
		}

		var created = new Node();
		node.Params.Add((p.Name, p.Type, created));
		node.Params.Sort((a, b) => a.Type.Priority.CompareTo(b.Type.Priority));
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

		node.CatchAll = (c.Name, handler);
		return node;
	}

	/// <summary>
	/// Match an HTTP method and path against the routes.
	/// </summary>
	/// <param name="method">HTTP method.</param>
	/// <param name="path">Request path.</param>
	/// <returns>The match result.</returns>
	public RouteMatch Match(string method, string path) =>
		throw new NotImplementedException();

	/// <summary>
	/// Prevent any more routes being added.
	/// </summary>
	internal void Lock() =>
		locked = true;
}
