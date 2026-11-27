// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using Microsoft.AspNetCore.Http;
using Wrap;

namespace Merlin.Domain;

public sealed record class Method : Monad<Method, string>, IComparable<Method>
{
	public static readonly Method Connect =
		Wrap(HttpMethods.Connect);

	public static readonly Method Delete =
		Wrap(HttpMethods.Delete);

	public static readonly Method Get =
		Wrap(HttpMethods.Get);

	public static readonly Method Head =
		Wrap(HttpMethods.Head);

	public static readonly Method Options =
		Wrap(HttpMethods.Options);

	public static readonly Method Patch =
		Wrap(HttpMethods.Patch);

	public static readonly Method Post =
		Wrap(HttpMethods.Post);

	public static readonly Method Put =
		Wrap(HttpMethods.Put);

	public int CompareTo(Method? other) =>
		string.CompareOrdinal(Value, other?.Value);

	public static bool operator <(Method left, Method right) =>
		left is null ? right is not null : left.CompareTo(right) < 0;

	public static bool operator <=(Method left, Method right) =>
		left is null || left.CompareTo(right) <= 0;

	public static bool operator >(Method left, Method right) =>
		left is not null && left.CompareTo(right) > 0;

	public static bool operator >=(Method left, Method right) =>
		left is null ? right is null : left.CompareTo(right) >= 0;
}
