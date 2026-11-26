// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Merlin.Replies;

/// <summary>
/// A reply that writes UTF-8 plain text.
/// </summary>
/// <param name="Content">Text to write (null writes an empty body).</param>
public sealed record class TextReply(string? Content) : Reply("text/plain; charset=utf-8")
{
	/// <inheritdoc/>
	protected override Task WriteBodyAsync(Stream stream, JsonSerializerOptions json, CancellationToken cancellationToken) =>
		stream.WriteAsync(Encoding.UTF8.GetBytes(Content ?? string.Empty), cancellationToken).AsTask();
}
