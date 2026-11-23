// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Collections.Immutable;
using System.IO;
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

	public static Reply MethodNotAllowed(ImmutableArray<string> allowed) =>
		throw new NotImplementedException();

	public static Reply NotFound() =>
		throw new NotImplementedException();
}
