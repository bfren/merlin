// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using Wrap;

namespace Merlin.Extensions;

public static partial class ReqExtensions
{
	/// <summary>
	/// Get a typed route value captured when the route matched.
	/// </summary>
	/// <typeparam name="T">Value type - must match the parameter's type in the route template.</typeparam>
	/// <param name="this">Request.</param>
	/// <param name="name">Parameter name in the route template.</param>
	/// <returns>
	/// The value, or a failure if it is missing or is not a <typeparamref name="T"/>
	/// (meaning the handler and its route template disagree).
	/// </returns>
	public static Result<T> Route<T>(this Req @this, string name) =>
		@this.RouteValues.TryGetValue(name, out var x) switch
		{
			true when x is T y =>
				y,

			_ =>
				RouteValueIsIncorrectType(name, typeof(T))
		};

	[Failure("Route value {Name} is not a {Type}.")]
	private static partial Failure RouteValueIsIncorrectType(string name, Type type);
}
