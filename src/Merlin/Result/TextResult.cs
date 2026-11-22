// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text;
using System.Threading.Tasks;

namespace Merlin.Result;

public sealed record class TextResult(string? Content) : HttpResult("text/plain")
{
	public override Task<byte[]> GetContentAsync()
	{
		var bytes = Encoding.UTF8.GetBytes(Content ?? string.Empty);
		return Task.FromResult(bytes);
	}
}
