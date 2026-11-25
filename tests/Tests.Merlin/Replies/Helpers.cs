// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Wrap;

namespace Merlin.Replies;

public sealed record class TestDto(int Id, string Name);

[JsonSerializable(typeof(TestDto))]
public partial class TestJson : JsonSerializerContext;

internal static class Helpers
{
	/// <summary>
	/// Write a reply to a fresh response with a memory body.
	/// </summary>
	public static async Task<(Result<bool> Result, HttpResponse Response, string Body)> WriteAsync(Reply reply)
	{
		var response = new DefaultHttpContext().Response;
		var body = new MemoryStream();
		response.Body = body;

		var result = await reply.WriteAsync(response, TestContext.Current.CancellationToken);

		return (result, response, Encoding.UTF8.GetString(body.ToArray()));
	}
}
