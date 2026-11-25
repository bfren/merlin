// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Wrap.Testing;

namespace Merlin.Replies.Reply_Tests;

public class Json_Tests
{
	[Fact]
	public void Returns_JsonReply_With_Content_And_Info()
	{
		// Arrange
		var content = new TestDto(Rnd.Int, Rnd.Str);
		var info = TestJson.Default.TestDto;

		// Act
		var result = Reply.Json(content, info);

		// Assert
		var reply = Assert.IsType<JsonReply<TestDto>>(result.AssertOk());
		Assert.Same(content, reply.Content);
		Assert.Same(info, reply.Info);
		Assert.Equal(200, reply.StatusCode);
	}
}
