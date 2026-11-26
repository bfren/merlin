// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text.Json.Serialization;

namespace Merlin.Replies;

/// <summary>
/// Problem details for an HTTP error reply, as defined by RFC 9457 and written as <c>application/problem+json</c>.
/// </summary>
/// <remarks>
/// Member names and null handling are fixed by attributes, so the RFC 9457 format is the same whatever JSON options are used.
/// </remarks>
public sealed record class Problem
{
	/// <summary>
	/// Short, human-readable summary of the problem type, e.g. <c>Not Found</c>.
	/// </summary>
	[JsonPropertyName("title")]
	public required string Title { get; init; }

	/// <summary>
	/// HTTP status code for this occurrence of the problem.
	/// </summary>
	[JsonPropertyName("status")]
	public required int Status { get; init; }

	/// <summary>
	/// [Optional] URI reference identifying the problem type - when left out, the type is <c>about:blank</c>.
	/// </summary>
	[JsonPropertyName("type")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Type { get; init; } =
		null;

	/// <summary>
	/// [Optional] Human-readable explanation of this occurrence of the problem - never include internal details.
	/// </summary>
	[JsonPropertyName("detail")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Detail { get; init; } =
		null;

	/// <summary>
	/// [Optional] URI reference identifying this occurrence of the problem.
	/// </summary>
	[JsonPropertyName("instance")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Instance { get; init; } =
		null;
}

/// <summary>
/// Source-generated JSON metadata for Merlin's own types (AOT-safe) - added to every app's JSON options by <see cref="App"/>.
/// </summary>
/// <remarks>
/// Has no options of its own: Merlin's types fix their wire format with attributes, so they work with any options.
/// </remarks>
[JsonSerializable(typeof(Problem))]
internal partial class MerlinJson : JsonSerializerContext;
