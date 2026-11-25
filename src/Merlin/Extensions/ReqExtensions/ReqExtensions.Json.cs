// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Wrap;

namespace Merlin.Extensions;

public static partial class ReqExtensions
{
	/// <summary>
	/// Read the request body as JSON, resolving <typeparamref name="T"/>'s type information
	/// from <see cref="Req.JsonOptions"/>.
	/// </summary>
	/// <typeparam name="T">Value type - must be registered with one of the JSON options' resolvers.</typeparam>
	/// <param name="this">Request.</param>
	/// <returns>
	/// The deserialised value, or a failure if the type is not registered, the body is not JSON,
	/// is empty, or cannot be deserialised.
	/// </returns>
	public static Task<Result<T>> Json<T>(this Req @this) =>
		throw new NotImplementedException();

	/// <summary>
	/// Read the request body as JSON, using source-generated type information (AOT-safe).
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="this">Request.</param>
	/// <param name="info">Source-generated type information for <typeparamref name="T"/>.</param>
	/// <returns>
	/// The deserialised value, or a failure if the body is not JSON, is empty, or cannot be deserialised.
	/// </returns>
	public static Task<Result<T>> Json<T>(this Req @this, JsonTypeInfo<T> info) =>
		throw new NotImplementedException();
}
