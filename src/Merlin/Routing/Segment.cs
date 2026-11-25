// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Routing;

/// <summary>
/// One part of a parsed route template: <see cref="Literal"/>, <see cref="Param"/> or <see cref="CatchAll"/>.
/// </summary>
public closed record class Segment;
