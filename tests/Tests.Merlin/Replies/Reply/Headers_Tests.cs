// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Replies.Reply_Tests;

public class Headers_Tests
{
	[Fact]
	public void Has_Default_Headers()
	{
		// Arrange

		// Act
		var result = new TextReply(Rnd.Str).Headers;

		// Assert
		Assert.Equal(3, result.Count);
		Assert.Equal("text/plain; charset=utf-8", result["Content-Type"]);
		Assert.Equal("Merlin", result["Server"]);
		Assert.Equal("GNU Terry Pratchett", result["X-Clacks-Overhead"]);
	}

	[Fact]
	public void Content_Type_Depends_On_Reply()
	{
		// Arrange

		// Act
		var result = new JsonReply<TestDto>(new(Rnd.Int, Rnd.Str)).Headers;

		// Assert
		Assert.Equal("application/json", result["Content-Type"]);
	}

	[Fact]
	public void With_Does_Not_Change_Original_Headers()
	{
		// Arrange
		var original = new TextReply(Rnd.Str);
		var name = Rnd.Str;

		// Act
		var copy = original with { Headers = original.Headers.SetItem(name, Rnd.Str).SetItem("Server", Rnd.Str) };

		// Assert
		Assert.False(original.Headers.ContainsKey(name));
		Assert.Equal("Merlin", original.Headers["Server"]);
		Assert.True(copy.Headers.ContainsKey(name));
	}

	[Fact]
	public void With_Status_Keeps_Headers()
	{
		// Arrange
		var original = new TextReply(Rnd.Str);

		// Act
		var copy = original with { StatusCode = 201 };

		// Assert
		Assert.Equal(original.Headers, copy.Headers);
		Assert.Equal(200, original.StatusCode);
	}
}
