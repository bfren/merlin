// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Wrap;

namespace Merlin.Replies;

public sealed record class TestDto(int Id, string Name);

/// <summary>
/// Not registered with <see cref="TestJson"/>.
/// </summary>
public sealed record class UnregisteredDto(int Id);

[JsonSerializable(typeof(TestDto))]
public partial class TestJson : JsonSerializerContext;

internal static class Helpers
{
	/// <summary>
	/// The options a real app has when it is given <see cref="TestJson"/>.
	/// </summary>
	public static JsonSerializerOptions Json { get; } =
		new App([], TestJson.Default).Json;

	/// <summary>
	/// Write a reply to a fresh response with a memory body, using <see cref="Json"/>.
	/// </summary>
	public static async Task<(Result<bool> Result, HttpResponse Response, string Body)> WriteAsync(Reply reply)
	{
		var response = new DefaultHttpContext().Response;
		var body = new MemoryStream();
		response.Body = body;

		var result = await reply.WriteAsync(response, Json, TestContext.Current.CancellationToken);

		return (result, response, Encoding.UTF8.GetString(body.ToArray()));
	}
}
