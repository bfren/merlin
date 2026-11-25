// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Routing;

/// <summary>
/// Matches fixed text, e.g. <c>posts</c> (case-insensitive).
/// </summary>
/// <param name="Value">Text to match.</param>
public sealed record class Literal(string Value) : Segment;
