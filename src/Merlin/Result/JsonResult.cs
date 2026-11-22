// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text;
using System.Threading.Tasks;

namespace Merlin.Result;

public sealed record class JsonResult(object Content) : HttpResult("application/json")
{
	public override Task<byte[]> GetContentAsync()
	{
		var json = System.Text.Json.JsonSerializer.Serialize(Content);
		var bytes = Encoding.UTF8.GetBytes(json);
		return Task.FromResult(bytes);
	}
}
