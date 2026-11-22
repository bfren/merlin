// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Merlin.Replies;

public sealed record class JsonReply<T>(T Content, JsonTypeInfo<T> Info) : Reply("application/json")
{
	protected override Task WriteBodyAsync(Stream stream, CancellationToken cancellationToken) =>
		JsonSerializer.SerializeAsync(stream, Content, Info, cancellationToken);
}
