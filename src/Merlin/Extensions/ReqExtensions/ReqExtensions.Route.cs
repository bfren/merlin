// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Wrap;

namespace Merlin.Extensions;

public static partial class ReqExtensions
{
	public static Result<T> Route<T>(this Req @this, string name) =>
		@this.RouteValues.TryGetValue(name, out var x) switch
		{
			true when x is T y =>
				y,

			_ =>
				R.Fail("Route value {Name} is not a {Type}.", name, typeof(T).Name)
					.Ctx(nameof(ReqExtensions), nameof(Route))
		};
}
