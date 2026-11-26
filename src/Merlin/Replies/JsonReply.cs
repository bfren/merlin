// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Merlin.Replies;

/// <summary>
/// A reply that serialises a value as JSON, using the source-generated metadata registered with the application's JSON options (AOT-safe).
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
/// <param name="Content">Value to serialise.</param>
public sealed record class JsonReply<T>(T Content) : Reply("application/json")
{
	/// <inheritdoc/>
	protected override Task WriteBodyAsync(Stream stream, JsonSerializerOptions json, CancellationToken cancellationToken) =>
		JsonSerializer.SerializeAsync(stream, Content, json.GetTypeInfo<T>(), cancellationToken);
}
