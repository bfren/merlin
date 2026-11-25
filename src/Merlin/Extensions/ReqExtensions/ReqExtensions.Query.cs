// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using Wrap;

namespace Merlin.Extensions;

public static partial class ReqExtensions
{
	/// <summary>
	/// Get a required query string value, parsed as <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="this">Request.</param>
	/// <param name="name">Query string key.</param>
	/// <returns>The parsed value, or a failure if the key is missing or its value cannot be parsed.</returns>
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

	/// <summary>
	/// Get an optional query string value, parsed as <typeparamref name="T"/>.
	/// </summary>
	/// <remarks>
	/// A value that cannot be parsed is treated the same as a missing value.
	/// </remarks>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="this">Request.</param>
	/// <param name="name">Query string key.</param>
	/// <returns>The parsed value, or None if the key is missing or its value cannot be parsed.</returns>
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
