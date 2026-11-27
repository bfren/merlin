// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text.Json;
using Merlin.Domain;
using Microsoft.AspNetCore.Http;
using Wrap.Testing;
using static Merlin.Replies.Helpers;

namespace Merlin.Replies.Reply_Tests;

public class MethodNotAllowed_Tests
{
	[Fact]
	public void Returns_405()
	{
		// Arrange

		// Act
		var result = Reply.MethodNotAllowed([Method.Get]);

		// Assert
		Assert.Equal(Status.MethodNotAllowed, result.AssertOk().Status);
	}

	[Fact]
	public void Sets_Allow_Header_From_Allowed_Methods()
	{
		// Arrange

		// Act
		var result = Reply.MethodNotAllowed([Method.Get, Method.Put]);

		// Assert
		Assert.Equal("GET, PUT", result.AssertOk().Headers["Allow"]);
	}

	[Fact]
	public async Task Writes_Allow_Header()
	{
		// Arrange
		var reply = Reply.MethodNotAllowed([Method.Get, Method.Put]).AssertOk();

		// Act
		var (_, response, _) = await WriteAsync(reply);

		// Assert
		Assert.Equal("GET, PUT", response.Headers.Allow);
	}

	[Fact]
	public async Task Writes_Allowed_Methods_As_Detail()
	{
		// Arrange
		var reply = Reply.MethodNotAllowed([Method.Get, Method.Put]).AssertOk();

		// Act
		var (_, _, body) = await WriteAsync(reply);

		// Assert
		Assert.Equal("Allowed methods: GET, PUT", JsonElement.Parse(body).GetProperty("detail").GetString());
	}
}
