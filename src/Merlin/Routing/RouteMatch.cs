// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Routing;

/// <summary>
/// The result of matching a request against the routes: <see cref="Matched"/>,
/// <see cref="MethodNotAllowed"/> or <see cref="NotFound"/>.
/// </summary>
public closed record class RouteMatch;
