// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Collections.Generic;

namespace Merlin.Routing;

public sealed class Router
{
	private bool locked;

	private readonly Dictionary<string, Node> tries =
		[with(StringComparer.OrdinalIgnoreCase)];

	private readonly Dictionary<string, Template> names =
		[with(StringComparer.Ordinal)];

	public ParamTypes Types { get; } =
		new();

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

	private void EnsureUnlocked()
	{
		if (locked)
		{
			throw new InvalidOperationException("Routes are locked - add new routes before the app starts.");
		}
	}

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

	private static Node SetCatchAll(Node node, CatchAll c, Handler handler, string source)
	{
		if (node.CatchAll is not null)
		{
			throw new InvalidOperationException($"'{source}': catch-all already mapped here.");
		}

		node.CatchAll = (c.Name, handler);
		return node;
	}

	public RouteMatch Match(string method, string path) =>
		throw new NotImplementedException();

	internal void Lock() =>
		locked = true;
}
