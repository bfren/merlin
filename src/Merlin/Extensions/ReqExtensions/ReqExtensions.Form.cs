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
	/// Read the request body as a form, mapping its fields onto <typeparamref name="T"/>'s properties
	/// through its JSON metadata, so naming policy, <c>[JsonPropertyName]</c>, init-only properties
	/// and constructors behave as they do for JSON.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="this">Request.</param>
	/// <param name="info">Source-generated type information for <typeparamref name="T"/>.</param>
	/// <returns>
	/// The bound value, or a failure if the body is not a form or a field cannot be converted.
	/// </returns>
	public static Task<Result<T>> Form<T>(this Req @this, JsonTypeInfo<T> info) =>
		throw new NotImplementedException();
}
