// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using Wrap;

namespace Merlin.Extensions;

public static partial class ReqExtensions
{
	public static Result<T> Query<T>(this Req @this, string name)
		where T : IParsable<T> =>
		@this.Query.TryGetValue(name, out var x) switch
		{
			true when T.TryParse(x.ToString(), F.DefaultCulture, out var y) =>
				y,

			_ =>
				R.Fail("Query value {Name} is missing or not a valid {Type}.", name, typeof(T).Name)
					.Ctx(nameof(ReqExtensions), nameof(Query))
		};

	public static Maybe<T> QueryOrNone<T>(this Req @this, string name)
		where T : IParsable<T> =>
		@this.Query.TryGetValue(name, out var x) switch
		{
			true when T.TryParse(x.ToString(), F.DefaultCulture, out var y) =>
				y,

			_ =>
				M.None
		};
}
