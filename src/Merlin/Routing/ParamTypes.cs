// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Collections.Generic;

namespace Merlin.Routing;

/// <summary>
/// The param types route templates can use: built-in <c>int</c>, <c>long</c>, <c>guid</c>, <c>bool</c>,
/// <c>dateonly</c> and <c>string</c> (the default for an untyped <c>{name}</c>), plus any that are added.
/// </summary>
public sealed class ParamTypes
{
	private readonly Dictionary<string, ParamType> types = new(StringComparer.OrdinalIgnoreCase)
	{
		["int"] = ParamType.Create<int>("int", 10),
		["long"] = ParamType.Create<long>("long", 20),
		["guid"] = ParamType.Create<Guid>("guid", 10),
		["bool"] = ParamType.Create<bool>("bool", 10),
		["dateonly"] = ParamType.Create<DateOnly>("dateonly", 10),
		["string"] = ParamType.Create<string>("string", 100),   // the default for {name}
	};

	/// <summary>
	/// Add a param type.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="name">Name used in route templates, e.g. <c>postid</c> for <c>{id:postid}</c>.</param>
	/// <param name="priority">Order among sibling parameters - lower is tried first (built-in types use 10-20, <c>string</c> 100).</param>
	/// <exception cref="InvalidOperationException"><paramref name="name"/> is reserved.</exception>
	/// <exception cref="ArgumentException">A param type called <paramref name="name"/> already exists.</exception>
	public void Add<T>(string name, int priority = 50) where T : IParsable<T>
	{
		// check for reserved names to avoid breaking segment parsing
		var reserved = new[] { "*" };
		if (reserved.Contains(name))
		{
			throw new InvalidOperationException($"Route param types cannot be {string.Join(',', reserved)}.");
		}

		// throws on adding a duplicate
		types.Add(name, ParamType.Create<T>(name, priority));
	}

	/// <summary>
	/// Get a param type by name.
	/// </summary>
	/// <param name="name">Param type name.</param>
	/// <returns>The param type.</returns>
	/// <exception cref="InvalidOperationException">No param type called <paramref name="name"/> exists.</exception>
	internal ParamType Get(string name) =>
		types.TryGetValue(name, out var t)
			? t
			: throw new InvalidOperationException($"Unknown route param type '{name}'.");
}
