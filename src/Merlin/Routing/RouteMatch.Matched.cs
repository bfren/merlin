// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Domain;

namespace Merlin.Routing;

/// <summary>
/// The request matched a route.
/// </summary>
/// <param name="Handler">The route's handler.</param>
/// <param name="Values">Typed values captured from the path, keyed by parameter name.</param>
public sealed record class Matched(Handler Handler, RouteValuesDictionary Values) : RouteMatch;
