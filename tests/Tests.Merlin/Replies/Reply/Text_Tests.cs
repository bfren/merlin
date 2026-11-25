// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Wrap.Testing;

namespace Merlin.Replies.Reply_Tests;

public class Text_Tests
{
	[Fact]
	public void Returns_TextReply_With_Content()
	{
		// Arrange
		var content = Rnd.Str;

		// Act
		var result = Reply.Text(content);

		// Assert
		var reply = Assert.IsType<TextReply>(result.AssertOk());
		Assert.Equal(content, reply.Content);
		Assert.Equal(200, reply.StatusCode);
	}
}
