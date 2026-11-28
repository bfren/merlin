// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Collections.Generic;
using Merlin.Domain;

namespace Merlin.Routing;

/// <summary>
/// The route table: one trie per HTTP method, built from route templates at startup
/// and walked for each request.
/// </summary>
public sealed partial class Router
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
	/// Resolve every route's handler to a <see cref="HandlerAsync"/> (calling each <see cref="HandlerFactory"/> once),
	/// then prevent any more routes being added.
	/// </summary>
	/// <param name="sp">The application's root service provider, passed to each handler factory.</param>
	/// <exception cref="InvalidOperationException">The router is already locked, or a handler factory failed.</exception>
	internal void Lock(IServiceProvider sp)
	{
		EnsureUnlocked();

		foreach (var (method, root) in tries)
		{
			Resolve(method, root, sp);
		}

		locked = true;
	}
}
