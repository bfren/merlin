// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
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
	/// <param name="json">The application's JSON options - replies without a JSON body ignore them.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task that completes when the body has been written.</returns>
	protected abstract Task WriteBodyAsync(Stream stream, JsonSerializerOptions json, CancellationToken cancellationToken);

	#region Static Helpers

	/// <summary>
	/// Create an error reply for a failure.
	/// </summary>
	/// <param name="failure">Failure.</param>
	/// <returns>An error reply.</returns>
	public static Result<Reply> Error(Failure failure) =>
		throw new NotImplementedException();

	/// <summary>
	/// Create a JSON reply, serialised when it is written using the application's JSON options.
	/// </summary>
	/// <typeparam name="T">Value type - must be registered with one of the application's JSON resolvers.</typeparam>
	/// <param name="content">Value to serialise.</param>
	/// <returns>A JSON reply.</returns>
	public static Result<Reply> Json<T>(T content) =>
		new JsonReply<T>(content);

	/// <summary>
	/// Create a 405 Method Not Allowed reply, with an Allow header listing the methods the path does support.
	/// </summary>
	/// <param name="allowed">Methods allowed for the path.</param>
	/// <returns>A 405 reply.</returns>
	public static Result<Reply> MethodNotAllowed(ImmutableArray<Method> allowed) =>
		throw new NotImplementedException();

	/// <summary>
	/// Create a 404 Not Found reply.
	/// </summary>
	/// <returns>A 404 reply.</returns>
	public static Result<Reply> NotFound() =>
		throw new NotImplementedException();

	/// <summary>
	/// Create an RFC 9457 problem details reply, with the problem's status code and an <c>application/problem+json</c> content type.
	/// </summary>
	/// <param name="problem">Problem details.</param>
	/// <returns>A problem details reply.</returns>
	public static Result<Reply> Problem(Problem problem)
	{
		var reply = new JsonReply<Problem>(problem);
		return reply with
		{
			Headers = reply.Headers.SetItem("Content-Type", "application/problem+json"),
			StatusCode = problem.Status
		};
	}

	/// <summary>
	/// Create a plain-text reply.
	/// </summary>
	/// <param name="content">Text to write.</param>
	/// <returns>A plain-text reply.</returns>
	public static Result<Reply> Text(string content) =>
		new TextReply(content);

	#endregion
}
