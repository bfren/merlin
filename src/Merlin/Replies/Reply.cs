// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Merlin.Domain;
using Microsoft.AspNetCore.Http;
using Wrap;

namespace Merlin.Replies;

/// <summary>
/// Base for all replies: holds the status code and headers, and writes them before the body.
/// </summary>
public abstract partial record class Reply
{
	/// <summary>
	/// HTTP status code (default 200 OK).
	/// </summary>
	public Status Status { get; init; } =
		Status.Ok;

	/// <summary>
	/// Response headers - each is set (not appended) on the response, so every name appears once.
	/// </summary>
	public ImmutableDictionary<string, string> Headers { get; init; }

	/// <summary>
	/// Create a reply with the given content type and Merlin's default headers.
	/// </summary>
	/// <param name="contentType">Value of the Content-Type header.</param>
	protected Reply(string contentType) =>
		Headers = ImmutableDictionary<string, string>.Empty
			.Add("Content-Type", contentType)
			.Add("Server", "Merlin")
			.Add("X-Clacks-Overhead", "GNU Terry Pratchett");

	/// <summary>
	/// Write the status code, headers and body to the response.
	/// </summary>
	/// <param name="response">HTTP response.</param>
	/// <param name="json">The application's JSON options, used by replies with a JSON body.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>
	/// True once the body has been written, or a failure if writing threw an exception
	/// (including a JSON body whose type is not registered with <paramref name="json"/>).
	/// </returns>
	public Task<Result<bool>> WriteAsync(HttpResponse response, JsonSerializerOptions json, CancellationToken cancellationToken)
	{
		SetHeaders(response);
		return R.TryAsync(async () =>
		{
			await WriteBodyAsync(response.Body, json, cancellationToken);
			return true;
		});
	}

	/// <summary>
	/// Copy the status code and headers onto the response.
	/// </summary>
	/// <param name="response">HTTP response.</param>
	protected virtual void SetHeaders(HttpResponse response)
	{
		response.StatusCode = Status.Value;
		foreach (var item in Headers)
		{
			response.Headers[item.Key] = item.Value;
		}
	}

	/// <summary>
	/// Write the reply body to the response stream.
	/// </summary>
	/// <param name="stream">Response body stream.</param>
	/// <param name="json">The application's JSON options - replies without a JSON body ignore them.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task that completes when the body has been written.</returns>
	protected abstract Task WriteBodyAsync(Stream stream, JsonSerializerOptions json, CancellationToken cancellationToken);
}
