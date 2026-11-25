// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text;
using static Merlin.Replies.Helpers;

namespace Merlin.Replies.TextReply_Tests;

public class WriteBodyAsync_Tests
{
	[Fact]
	public async Task Writes_Content()
	{
		// Arrange
		var content = Rnd.Str;

		// Act
		var (_, _, body) = await WriteAsync(new TextReply(content));

		// Assert
		Assert.Equal(content, body);
	}

	[Fact]
	public async Task Writes_UTF8_Without_BOM()
	{
		// Arrange
		var content = "Café ☕ 日本";
		var response = new Microsoft.AspNetCore.Http.DefaultHttpContext().Response;
		var stream = new MemoryStream();
		response.Body = stream;

		// Act
		await new TextReply(content).WriteAsync(response, TestContext.Current.CancellationToken);

		// Assert
		Assert.Equal(Encoding.UTF8.GetBytes(content), stream.ToArray());
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public async Task Writes_Empty_Body_For_Null_Or_Empty(string? content)
	{
		// Arrange

		// Act
		var (_, _, body) = await WriteAsync(new TextReply(content));

		// Assert
		Assert.Empty(body);
	}
}
