// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;

namespace Merlin.Domain;

public sealed record class RouteValue(string Name, object Value) : IComparable<RouteValue>
{
	public int CompareTo(RouteValue? other) =>
		string.CompareOrdinal(Name, other?.Name);

	public static bool operator <(RouteValue left, RouteValue right) =>
		left is null ? right is not null : left.CompareTo(right) < 0;

	public static bool operator <=(RouteValue left, RouteValue right) =>
		left is null || left.CompareTo(right) <= 0;

	public static bool operator >(RouteValue left, RouteValue right) =>
		left is not null && left.CompareTo(right) > 0;

	public static bool operator >=(RouteValue left, RouteValue right) =>
		left is null ? right is null : left.CompareTo(right) >= 0;
}
