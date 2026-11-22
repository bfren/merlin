// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Merlin.Replies;

public sealed record class TextReply(string? Content) : Reply("text/plain; charset=utf-8")
{
	protected override Task WriteBodyAsync(Stream stream, CancellationToken cancellationToken) =>
		stream.WriteAsync(Encoding.UTF8.GetBytes(Content ?? string.Empty), cancellationToken).AsTask();
}
