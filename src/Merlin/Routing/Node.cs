// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Collections.Generic;

namespace Merlin.Routing;

/// <summary>
/// One point in a route trie: the ways onward from here (literal, parameter and catch-all children),
/// and the handler of a route that ends here.
/// </summary>
internal sealed class Node
{
	/// <summary>
	/// Literal children, keyed by segment text (case-insensitive).
	/// </summary>
	public Dictionary<string, Node> Literals { get; } =
		new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Parameter children, sorted by <see cref="ParamType.Priority"/> so lower values are tried first.
	/// </summary>
	public List<(string Name, ParamType Type, Node Node)> Params { get; } =
		[];

	/// <summary>
	/// Catch-all route at this point, which captures the rest of the path.
	/// </summary>
	public (string Name, Handler Handler)? CatchAll { get; set; }

	/// <summary>
	/// Handler of a route that ends at this node.
	/// </summary>
	public Handler? Handler { get; set; }
}
