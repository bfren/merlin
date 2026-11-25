// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Microsoft.AspNetCore.Http;
using Wrap;
using Wrap.Testing;
using static Merlin.Replies.Helpers;

namespace Merlin.Replies.Reply_Tests;

public class Error_Tests
{
	[Fact]
	public void Returns_500()
	{
		// Arrange
		var failure = R.Fail(Rnd.Str);

		// Act
		var result = Reply.Error(failure);

		// Assert
		Assert.Equal(StatusCodes.Status500InternalServerError, result.AssertOk().StatusCode);
	}

	[Fact]
	public async Task Does_Not_Write_Failure_Details()
	{
		// Arrange
		var message = Rnd.Str;
		var (type, function) = (Rnd.Str, Rnd.Str);
		var failure = R.Fail(message).Ctx(type, function);
		var reply = Reply.Error(failure).AssertOk();

		// Act
		var (_, _, body) = await WriteAsync(reply);

		// Assert
		Assert.DoesNotContain(message, body);
		Assert.DoesNotContain(type, body);
		Assert.DoesNotContain(function, body);
	}
}
