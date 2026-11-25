// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Routing;

/// <summary>
/// Matches the rest of the path, e.g. <c>{path:*}</c> - must be the last segment.
/// </summary>
/// <param name="Name">Parameter name.</param>
public sealed record class CatchAll(string Name) : Segment;
