// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Collections.Generic;

namespace Merlin.Routing;

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

	public void Add<T>(string name, int priority = 50) where T : IParsable<T> =>
		types.Add(name, ParamType.Create<T>(name, priority));   // throws on duplicates

	internal ParamType Get(string name) =>
		types.TryGetValue(name, out var t)
			? t
			: throw new InvalidOperationException($"Unknown route type '{name}'.");
}
