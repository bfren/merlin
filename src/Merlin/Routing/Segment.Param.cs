// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Routing;

/// <summary>
/// Matches one path segment that can be parsed as its type, e.g. <c>{id:int}</c>.
/// </summary>
/// <param name="Name">Parameter name.</param>
/// <param name="Type">Parameter type.</param>
public sealed record class Param(string Name, ParamType Type) : Segment;
