// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Collections.Generic;

namespace Merlin.Routing;

public sealed class Router
{
	private bool locked;

	private readonly Dictionary<string, Node> roots =
		new(StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<string, Template> names =
		new(StringComparer.Ordinal);

	public ParamTypes Types { get; } = new();

	internal void Add(string method, Template template, Handler handler)
	{
		if (locked)
		{
			throw new InvalidOperationException("Routes are locked - add new routes before the app starts.");
		}
	}

	public RouteMatch Match(string method, string path) =>
		throw new NotImplementedException();

	internal void Lock() =>
		locked = true;
}
