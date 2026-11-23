// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Collections.Immutable;

namespace Merlin.Routing;

public sealed record class Template(string Source, ImmutableArray<Segment> Segments)
{
	public static Template Parse(string source, ParamTypes types) =>
		throw new NotImplementedException();
}
