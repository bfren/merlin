// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Merlin.Replies;

/// <summary>
/// A reply that serialises a value as JSON, using source-generated metadata (AOT-safe).
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
/// <param name="Content">Value to serialise.</param>
/// <param name="Info">Source-generated type information for <typeparamref name="T"/>.</param>
public sealed record class JsonReply<T>(T Content, JsonTypeInfo<T> Info) : Reply("application/json")
{
	/// <inheritdoc/>
	protected override Task WriteBodyAsync(Stream stream, CancellationToken cancellationToken) =>
		JsonSerializer.SerializeAsync(stream, Content, Info, cancellationToken);
}
