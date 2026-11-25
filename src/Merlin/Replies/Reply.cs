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

/// <summary>
/// Base for all replies: holds the status code and headers, and writes them before the body.
/// </summary>
public abstract record class Reply
{
	/// <summary>
	/// HTTP status code (default 200 OK).
	/// </summary>
	public int StatusCode { get; init; } =
		StatusCodes.Status200OK;

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
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>True once the body has been written, or a failure if writing threw an exception.</returns>
	public Task<Result<bool>> WriteAsync(HttpResponse response, CancellationToken cancellationToken)
	{
		SetHeaders(response);
		return R.TryAsync(async () =>
		{
			await WriteBodyAsync(response.Body, cancellationToken);
			return true;
		});
	}

	/// <summary>
	/// Copy the status code and headers onto the response.
	/// </summary>
	/// <param name="response">HTTP response.</param>
	protected virtual void SetHeaders(HttpResponse response)
	{
		response.StatusCode = StatusCode;
		foreach (var item in Headers)
		{
			response.Headers[item.Key] = item.Value;
		}
	}

	/// <summary>
	/// Write the reply body to the response stream.
	/// </summary>
	/// <param name="stream">Response body stream.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task that completes when the body has been written.</returns>
	protected abstract Task WriteBodyAsync(Stream stream, CancellationToken cancellationToken);

	#region Static Helpers

	/// <summary>
	/// Create an error reply for a failure.
	/// </summary>
	/// <param name="failure">Failure.</param>
	/// <returns>An error reply.</returns>
	public static Result<Reply> Error(Failure failure) =>
		throw new NotImplementedException();

	/// <summary>
	/// Create a JSON reply.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="content">Value to serialise.</param>
	/// <param name="info">Source-generated type information for <typeparamref name="T"/>.</param>
	/// <returns>A JSON reply.</returns>
	public static Result<Reply> Json<T>(T content, JsonTypeInfo<T> info) =>
		new JsonReply<T>(content, info);

	/// <summary>
	/// Create a 405 Method Not Allowed reply, with an Allow header listing the methods the path does support.
	/// </summary>
	/// <param name="allowed">Methods allowed for the path.</param>
	/// <returns>A 405 reply.</returns>
	public static Result<Reply> MethodNotAllowed(ImmutableArray<string> allowed) =>
		throw new NotImplementedException();

	/// <summary>
	/// Create a 404 Not Found reply.
	/// </summary>
	/// <returns>A 404 reply.</returns>
	public static Result<Reply> NotFound() =>
		throw new NotImplementedException();

	/// <summary>
	/// Create a plain-text reply.
	/// </summary>
	/// <param name="content">Text to write.</param>
	/// <returns>A plain-text reply.</returns>
	public static Result<Reply> Text(string content) =>
		new TextReply(content);

	#endregion
}
