// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Wrap;

namespace Merlin.Replies;

public abstract record class Reply
{
	public int StatusCode { get; init; } =
		StatusCodes.Status200OK;

	public ImmutableDictionary<string, string> Headers { get; init; }

	protected Reply(string contentType) =>
		Headers = ImmutableDictionary<string, string>.Empty
			.Add("Content-Type", contentType)
			.Add("Server", "Merlin")
			.Add("X-Clacks-Overhead", "GNU Terry Pratchett");

	public Task<Result<bool>> WriteAsync(HttpResponse response, CancellationToken cancellationToken)
	{
		SetHeaders(response);
		return R.TryAsync(async () =>
		{
			await WriteBodyAsync(response.Body, cancellationToken);
			return true;
		});
	}

	protected virtual void SetHeaders(HttpResponse response)
	{
		response.StatusCode = StatusCode;
		foreach (var item in Headers)
		{
			response.Headers[item.Key] = item.Value;
		}
	}

	protected abstract Task WriteBodyAsync(Stream stream, CancellationToken cancellationToken);

	#region Static Helpers

	public static Result<Reply> Error(Failure failure) =>
		throw new NotImplementedException();

	public static Result<Reply> Json<T>(T content, JsonTypeInfo<T> info) =>
		new JsonReply<T>(content, info);

	public static Result<Reply> MethodNotAllowed(ImmutableArray<string> allowed) =>
		throw new NotImplementedException();

	public static Result<Reply> NotFound() =>
		throw new NotImplementedException();

	public static Result<Reply> Text(string content) =>
		new TextReply(content);

	#endregion
}
