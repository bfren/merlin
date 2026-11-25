// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text.Json.Serialization;

namespace Merlin.Extensions.ReqExtensions_Tests;

/// <summary>
/// Bound through its constructor; <see cref="AuthorName"/> has an explicit JSON name.
/// </summary>
public sealed record class PostDto(
	string? Title,
	int Views,
	bool Draft,
	string[]? Tags,
	[property: JsonPropertyName("author_name")] string? AuthorName
);

/// <summary>
/// Bound through init-only properties.
/// </summary>
public sealed class PostInitDto
{
	public string? Title { get; init; }

	public int Views { get; init; }
}

/// <summary>
/// Not registered with <see cref="BinderJson"/>.
/// </summary>
public sealed record class Unregistered(string Value);

/// <summary>
/// Default options apart from camelCase names - in particular numbers are not read from strings.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PostDto))]
[JsonSerializable(typeof(PostInitDto))]
public partial class BinderJson : JsonSerializerContext;
