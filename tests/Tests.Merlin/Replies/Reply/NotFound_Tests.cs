// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Domain;
using Microsoft.AspNetCore.Http;
using Wrap.Testing;

namespace Merlin.Replies.Reply_Tests;

public class NotFound_Tests
{
	[Fact]
	public void Returns_404()
	{
		// Arrange

		// Act
		var result = Reply.NotFound();

		// Assert
		Assert.Equal(Status.NotFound, result.AssertOk().Status);
	}
}
