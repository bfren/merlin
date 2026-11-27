// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Domain;

namespace Merlin.Routing;

/// <summary>
/// A catch-all route at a <see cref="Node"/>, which captures the rest of the path.
/// </summary>
/// <param name="Name">Parameter name, used as the key of the captured value.</param>
/// <param name="Handler">Handler of the catch-all route.</param>
internal readonly record struct NodeCatchAll(string Name, Handler Handler);
