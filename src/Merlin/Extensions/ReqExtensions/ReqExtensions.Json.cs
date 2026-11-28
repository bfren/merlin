// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Merlin.Exceptions;
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
		@this.JsonOptions.TryGetTypeInfo<T>(out var info)
			? Json(@this, info)
			: UnableToGetJsonTypeInfo(typeof(T)).AsTask<T>();

	[Failure("Unable to get JSON type info for {type} - please register it before startup.")]
	private static partial Failure UnableToGetJsonTypeInfo(Type type);

	/// <summary>
	/// Read the request body as JSON, using source-generated type information (AOT-safe).
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="this">Request.</param>
	/// <param name="info">Source-generated type information for <typeparamref name="T"/>.</param>
	/// <returns>
	/// The deserialised value, or a failure if the body is not JSON, is empty, or cannot be deserialised.
	/// </returns>
	public static Task<Result<T>> Json<T>(this Req @this, JsonTypeInfo<T> info)
	{
		var request = @this.Http.Request;

		if (request.ContentType != "application/json")
		{
			return R.Fail("Request is not JSON.")
				.WithKind(FailureKind.Invalid).AsTask<T>();
		}

		if (request.Body.Length == 0)
		{
			return R.Fail("Request body is empty.")
				.WithKind(FailureKind.Invalid).AsTask<T>();
		}

		try
		{
			return JsonSerializer.Deserialize(request.Body, info) switch
			{
				T value =>
					R.Wrap(value).AsTask(),

				_ =>
					throw new JsonNullException()
			};
		}
		catch (JsonException ex)
		{
			return InvalidJson(ex.Path ?? "$").AsTask<T>();
		}
		catch (Exception ex)
		{
			return R.Fail(ex).WithMessage("Something went wrong.").AsTask<T>();
		}
	}

	[Failure("Request body is not valid JSON at {Path}.", Kind = nameof(FailureKind.Invalid))]
	private static partial Failure InvalidJson(string path);
}
