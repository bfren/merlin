// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using static Merlin.Replies.Helpers;

namespace Merlin.Replies.JsonReply_Tests;

public class WriteBodyAsync_Tests
{
	[Fact]
	public async Task Writes_Content_As_Json()
	{
		// Arrange
		var id = Rnd.Int;
		var name = Rnd.Str;
		var reply = new JsonReply<TestDto>(new(id, name), TestJson.Default.TestDto);

		// Act
		var (_, _, body) = await WriteAsync(reply);

		// Assert
		Assert.Equal($$"""{"Id":{{id}},"Name":"{{name}}"}""", body);
	}

	[Fact]
	public async Task Writes_Null_Content_As_Json_Null()
	{
		// Arrange
		var reply = new JsonReply<TestDto>(null!, TestJson.Default.TestDto);

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
		var reply = new JsonReply<TestDto>(new(1, name), TestJson.Default.TestDto);

		// Act
		var (_, _, body) = await WriteAsync(reply);

		// Assert
		Assert.DoesNotContain("<", body);
		var parsed = System.Text.Json.JsonSerializer.Deserialize(body, TestJson.Default.TestDto);
		Assert.Equal(name, parsed?.Name);
	}
}
