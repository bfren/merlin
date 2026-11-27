// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Merlin.Domain;
using Microsoft.AspNetCore.WebUtilities;
using Wrap;
using Wrap.Extensions;

namespace Merlin.Replies;

public abstract partial record class Reply
{
	/// <summary>
	/// Create an error reply for a failure.
	/// </summary>
	/// <param name="failure">Failure.</param>
	/// <returns>An error reply.</returns>
	public static Result<Reply> Error(Failure failure)
	{
		var status = Statuses.GetValueOrDefault(failure.Kind, Status.InternalServerError);
		return Problem(new()
		{
			Title = ReasonPhrases.GetReasonPhrase(status.Value),
			Status = status.Value,
			Detail = status.Value < 500 && failure.Kind != FailureKind.Cancelled
				? Fill(failure.Message, failure.Args)
				: null
		});
	}

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
	public static Result<Reply> MethodNotAllowed(ImmutableArray<Method> allowed)
	{
		var methods = string.Join(", ", allowed.Select(m => m.Value));
		return Problem(
			new()
			{
				Title = ReasonPhrases.GetReasonPhrase(Status.MethodNotAllowed.Value),
				Status = Status.MethodNotAllowed.Value,
				Detail = $"Allowed methods: {methods}"
			}
		).Map(
			r => r with { Headers = r.Headers.SetItem("Allow", methods) }
		);
	}

	/// <summary>
	/// Create a 404 Not Found reply.
	/// </summary>
	/// <returns>A 404 reply.</returns>
	public static Result<Reply> NotFound() =>
		Problem(
			new()
			{
				Title = ReasonPhrases.GetReasonPhrase(Status.NotFound.Value),
				Status = Status.NotFound.Value
			}
		);

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
			Status = Status.Wrap(problem.Status)
		};
	}

	/// <summary>
	/// Create a plain-text reply.
	/// </summary>
	/// <param name="content">Text to write.</param>
	/// <returns>A plain-text reply.</returns>
	public static Result<Reply> Text(string content) =>
		new TextReply(content);

	private static readonly Dictionary<FailureKind, Status> Statuses = new()
	{
		[FailureKind.Invalid] = Status.BadRequest,
		[FailureKind.Unauthenticated] = Status.Unauthorised,
		[FailureKind.Forbidden] = Status.Forbidden,
		[FailureKind.NotFound] = Status.NotFound,
		[FailureKind.Conflict] = Status.Conflict,
		[FailureKind.Cancelled] = Status.ClientClosedRequest,
		[FailureKind.Unavailable] = Status.ServiceUnavailable,
		[FailureKind.Timeout] = Status.GatewayTimeout,
		// Unexpected, Unspecified and custom kinds fall through to 500
	};

	// Wrap messages are logging templates ("Value {Name} is not {Number}."), filled from Args in order
	private static string Fill(string message, object?[] args)
	{
		var i = 0;
		return Placeholder().Replace(message, m => i < args.Length ? $"{args[i++]}" : m.Value);
	}

	[GeneratedRegex(@"\{[^{}]+\}")] // source-generated, so AOT-safe
	private static partial Regex Placeholder();
}
