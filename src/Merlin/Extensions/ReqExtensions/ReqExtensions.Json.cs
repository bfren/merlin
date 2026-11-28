// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

	[Failure("Type {Type} is not registered for JSON.", Kind = nameof(FailureKind.Unexpected))]
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
	public static async Task<Result<T>> Json<T>(this Req @this, JsonTypeInfo<T> info)
	{
		var request = @this.Http.Request;

		if (!request.HasJsonContentType())
		{
			return RequestIsNotJson();
		}

		if (request.ContentLength == 0)
		{
			return RequestIsEmpty();
		}

		try
		{
			return await JsonSerializer.DeserializeAsync(request.Body, info, @this.Cancellation) switch
			{
				T value =>
					value,

				_ =>
					RequestIsEmpty()
			};
		}
		catch (JsonException ex)
		{
			return InvalidJson(ex.Path ?? "$");
		}
		catch (Exception ex)
		{
			return R.DefaultExceptionHandler(ex);
		}
	}

	[Failure("Request body is not JSON.", Kind = nameof(FailureKind.Invalid))]
	private static partial Failure RequestIsNotJson();

	[Failure("Request body is empty.", Kind = nameof(FailureKind.Invalid))]
	private static partial Failure RequestIsEmpty();

	[Failure("Request body is not valid JSON at {Path}.", Kind = nameof(FailureKind.Invalid))]
	private static partial Failure InvalidJson(string path);
}
