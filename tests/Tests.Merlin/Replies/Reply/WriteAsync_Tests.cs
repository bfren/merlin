// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Microsoft.AspNetCore.Http;
using Wrap.Testing;
using static Merlin.Replies.Helpers;

namespace Merlin.Replies.Reply_Tests;

public class WriteAsync_Tests
{
	public class Status
	{
		[Fact]
		public async Task Defaults_To_200()
		{
			// Arrange
			var reply = new TextReply(Rnd.Str);

			// Act
			var (_, response, _) = await WriteAsync(reply);

			// Assert
			Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
		}

		[Fact]
		public async Task Writes_Status_Set_With_With()
		{
			// Arrange
			var reply = new TextReply(Rnd.Str) with { StatusCode = StatusCodes.Status201Created };

			// Act
			var (_, response, _) = await WriteAsync(reply);

			// Assert
			Assert.Equal(StatusCodes.Status201Created, response.StatusCode);
		}
	}

	public class Headers
	{
		[Fact]
		public async Task Writes_Default_Headers()
		{
			// Arrange
			var reply = new TextReply(Rnd.Str);

			// Act
			var (_, response, _) = await WriteAsync(reply);

			// Assert
			Assert.Equal("text/plain; charset=utf-8", response.ContentType);
			Assert.Equal("Merlin", response.Headers.Server);
			Assert.Equal("GNU Terry Pratchett", response.Headers["X-Clacks-Overhead"]);
		}

		[Fact]
		public async Task Writes_Added_Header()
		{
			// Arrange
			var name = Rnd.Str;
			var value = Rnd.Str;
			var original = new TextReply(Rnd.Str);
			var reply = original with { Headers = original.Headers.Add(name, value) };

			// Act
			var (_, response, _) = await WriteAsync(reply);

			// Assert
			Assert.Equal(value, response.Headers[name]);
		}

		[Fact]
		public async Task Replaces_Existing_Header_Value()
		{
			// Arrange
			var response = new DefaultHttpContext().Response;
			response.Body = new MemoryStream();
			response.Headers.Server = "Kestrel";
			var reply = new TextReply(Rnd.Str);

			// Act
			await reply.WriteAsync(response, TestContext.Current.CancellationToken);

			// Assert
			var server = response.Headers.Server;
			Assert.Equal("Merlin", Assert.Single(server));
		}
	}

	public class Body
	{
		[Fact]
		public async Task Returns_True_When_Written()
		{
			// Arrange
			var reply = new TextReply(Rnd.Str);

			// Act
			var (result, _, _) = await WriteAsync(reply);

			// Assert
			result.AssertTrue();
		}

		[Fact]
		public async Task Returns_Failure_When_Stream_Throws()
		{
			// Arrange
			var response = new DefaultHttpContext().Response;
			var body = new MemoryStream();
			body.Dispose();
			response.Body = body;
			var reply = new TextReply(Rnd.Str);

			// Act
			var result = await reply.WriteAsync(response, TestContext.Current.CancellationToken);

			// Assert
			result.AssertFailure();
		}

		[Fact]
		public async Task Sets_Status_And_Headers_Before_Body_Fails()
		{
			// Arrange
			var response = new DefaultHttpContext().Response;
			var body = new MemoryStream();
			body.Dispose();
			response.Body = body;
			var reply = new TextReply(Rnd.Str) with { StatusCode = StatusCodes.Status202Accepted };

			// Act
			await reply.WriteAsync(response, TestContext.Current.CancellationToken);

			// Assert
			Assert.Equal(StatusCodes.Status202Accepted, response.StatusCode);
			Assert.Equal("Merlin", response.Headers.Server);
		}
	}
}
