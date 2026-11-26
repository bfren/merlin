// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Routing;

/// <summary>
/// A parameter child of a <see cref="Routing.Node"/>: the way onward for a segment that parses as <see cref="Type"/>.
/// </summary>
/// <param name="Name">Parameter name, used as the key of the captured value.</param>
/// <param name="Type">Parameter type, which parses the segment.</param>
/// <param name="Order">Order in which the parameter was added to its node, which breaks ties in <see cref="ParamType.Priority"/>.</param>
/// <param name="Node">Node reached when the segment parses.</param>
internal readonly record struct NodeParam(string Name, ParamType Type, int Order, Node Node);
