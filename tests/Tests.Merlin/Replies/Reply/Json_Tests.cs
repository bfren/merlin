// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Domain;
using Wrap.Testing;

namespace Merlin.Replies.Reply_Tests;

public class Json_Tests
{
	[Fact]
	public void Returns_JsonReply_With_Content()
	{
		// Arrange
		var content = new TestDto(Rnd.Int, Rnd.Str);

		// Act
		var result = Reply.Json(content);

		// Assert
		var reply = Assert.IsType<JsonReply<TestDto>>(result.AssertOk());
		Assert.Same(content, reply.Content);
		Assert.Equal(Status.Ok, reply.Status);
	}

	[Fact]
	public void Does_Not_Check_Registration_When_Created()
	{
		// Arrange
		var content = new UnregisteredDto(Rnd.Int);

		// Act
		var result = Reply.Json(content);

		// Assert
		Assert.IsType<JsonReply<UnregisteredDto>>(result.AssertOk());
	}
}
