// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Routing;

/// <summary>
/// No route matches the path.
/// </summary>
public sealed record class NotFound : RouteMatch;
