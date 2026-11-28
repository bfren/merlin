// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Routing;

/// <summary>
/// A catch-all route at a <see cref="Node"/>, which captures the rest of the path.
/// </summary>
/// <param name="Name">Parameter name, used as the key of the captured value.</param>
/// <param name="Route">The catch-all route: its handler and the template it came from.</param>
internal readonly record struct NodeCatchAll(string Name, NodeRoute Route);
