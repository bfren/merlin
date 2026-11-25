// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Collections.Generic;

namespace Merlin.Routing;

internal sealed class Node
{
	public Dictionary<string, Node> Literals { get; } =
		new(StringComparer.OrdinalIgnoreCase);

	public List<(string Name, ParamType Type, Node Node)> Params { get; } =
		[];

	public (string Name, Handler Handler)? CatchAll { get; set; }

	public Handler? Handler { get; set; }
}
