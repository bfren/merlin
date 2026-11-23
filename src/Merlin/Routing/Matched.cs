// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Collections.Immutable;

namespace Merlin.Routing;

public sealed record class Matched(Handler Handler, ImmutableDictionary<string, object> Values) : RouteMatch;
