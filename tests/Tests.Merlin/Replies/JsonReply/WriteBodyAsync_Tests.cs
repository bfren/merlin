// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text.Json;
using Wrap.Testing;
using static Merlin.Replies.Helpers;

namespace Merlin.Replies.JsonReply_Tests;

public class WriteBodyAsync_Tests
{
	[Fact]
	public async Task Writes_Content_With_App_Options()
	{
		// Arrange
		var id = Rnd.Int;
		var name = Rnd.Str;
		var reply = new JsonReply<TestDto>(new(id, name));

		// Act
		var (_, _, body) = await WriteAsync(reply);

		// Assert
		// camelCase names come from App.Json, not from TestJson (which sets no options)
		Assert.Equal($$"""{"id":{{id}},"name":"{{name}}"}""", body);
	}

	[Fact]
	public async Task Uses_The_Options_Passed_In()
	{
		// Arrange
		var reply = new JsonReply<TestDto>(new(1, "x"));
		var response = new Microsoft.AspNetCore.Http.DefaultHttpContext().Response;
		var stream = new MemoryStream();
		response.Body = stream;
		var options = new JsonSerializerOptions
		{
			TypeInfoResolver = TestJson.Default,
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper
		};

		// Act
		await reply.WriteAsync(response, options, TestContext.Current.CancellationToken);

		// Assert
		Assert.Equal("""{"ID":1,"NAME":"x"}""", System.Text.Encoding.UTF8.GetString(stream.ToArray()));
	}

	[Fact]
	public async Task Writes_Null_Content_As_Json_Null()
	{
		// Arrange
		var reply = new JsonReply<TestDto>(null!);

		// Act
		var (_, _, body) = await WriteAsync(reply);

		// Assert
		Assert.Equal("null", body);
	}

	[Fact]
	public async Task Escapes_Strings()
	{
		// Arrange
		var name = "\"<x>\"";
		var reply = new JsonReply<TestDto>(new(1, name));

		// Act
		var (_, _, body) = await WriteAsync(reply);

		// Assert
		Assert.DoesNotContain("<", body);
		var parsed = JsonSerializer.Deserialize<TestDto>(body, Json);
		Assert.Equal(name, parsed?.Name);
	}

	[Fact]
	public async Task Unregistered_Type_Returns_Failure()
	{
		// Arrange
		var reply = new JsonReply<UnregisteredDto>(new(Rnd.Int));

		// Act
		var (result, _, body) = await WriteAsync(reply);

		// Assert
		var failure = result.AssertFailure();
		Assert.IsType<NotSupportedException>(failure.Exception);
		Assert.Empty(body);
	}

	[Fact]
	public async Task Problem_Is_Registered_By_App()
	{
		// Arrange
		var reply = new JsonReply<Problem>(new() { Title = "Not Found", Status = 404 });

		// Act
		var (result, _, body) = await WriteAsync(reply);

		// Assert
		result.AssertTrue();
		Assert.Equal("""{"title":"Not Found","status":404}""", body);
	}
}
