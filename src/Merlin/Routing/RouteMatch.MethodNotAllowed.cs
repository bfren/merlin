// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Collections.Immutable;
using Merlin.Domain;

namespace Merlin.Routing;

/// <summary>
/// The path matched a route, but not for the request's HTTP method.
/// </summary>
/// <param name="Allowed">Methods the path does match, for the Allow header.</param>
public sealed record class MethodNotAllowed(ImmutableArray<Method> Allowed) : RouteMatch;
