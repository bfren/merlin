// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text.Json;
using Merlin.Replies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Wrap;
using static Merlin.Replies.Reply_Tests.Error_Tests.Status;

namespace Merlin.App_Tests;

public partial class StartAsync_Tests
{
	public class Failures
	{
		[Fact]
		public async Task Failure_Is_Logged_At_Its_Level()
		{
			// Arrange
			var message = Rnd.Str;
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(_ => R.Fail(message)));

			// Act
			_ = await host.SendAsync(HttpMethod.Get, "/");

			// Assert
			Assert.Contains(host.Logs, l => l.Level == LogLevel.Error && l.Message == message);
		}

		[Theory]
		[InlineData(nameof(FailureKind.Invalid), 400)]
		[InlineData(nameof(FailureKind.NotFound), 404)]
		[InlineData(nameof(FailureKind.Unexpected), 500)]
		public async Task Writes_Error_Reply_With_Status_From_Kind(string kind, int expected)
		{
			// Arrange
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(_ => R.Fail(Rnd.Str).WithKind(KindOf(kind))));

			// Act
			var (response, body, done) = await host.SendAsync(HttpMethod.Get, "/");

			// Assert
			Assert.Equal(expected, (int)response.StatusCode);
			Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
			Assert.Equal(expected, JsonElement.Parse(body).GetProperty("status").GetInt32());
			Assert.Null(done.Exception);
		}

		[Fact]
		public async Task Client_Error_Reply_Includes_Failure_Message_As_Detail()
		{
			// Arrange
			var message = Rnd.Str;
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(_ => R.Fail(message).WithKind(FailureKind.Invalid)));

			// Act
			var (_, body, _) = await host.SendAsync(HttpMethod.Get, "/");

			// Assert
			Assert.Equal(message, JsonElement.Parse(body).GetProperty("detail").GetString());
		}

		[Fact]
		public async Task Server_Error_Reply_Does_Not_Include_Failure_Message()
		{
			// Arrange
			var message = Rnd.Str;
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(_ => R.Fail(message)));

			// Act
			var (_, body, _) = await host.SendAsync(HttpMethod.Get, "/");

			// Assert
			Assert.DoesNotContain(message, body);
		}

		[Fact]
		public async Task After_Response_Started_Keeps_Response_And_Logs()
		{
			// Arrange
			var partial = Rnd.Str;
			var message = Rnd.Str;
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(async req =>
			{
				await req.Http.Response.WriteAsync(partial, req.Cancellation);
				return (Result<Reply>)R.Fail(message);
			}));

			// Act
			var (response, body, done) = await host.SendAsync(HttpMethod.Get, "/");

			// Assert
			Assert.Equal(200, (int)response.StatusCode);
			Assert.Equal(partial, body);
			Assert.Contains(host.Logs, l => l.Level == LogLevel.Error && l.Message == message);
			Assert.Null(done.Exception);
		}
	}
}
