// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text.Json;
using Merlin.Extensions;
using Merlin.Replies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Wrap;
using Wrap.Linq;
using static Merlin.Replies.Reply_Tests.Error_Tests.Status;

namespace Merlin.App_Tests;

public class StartAsync_Tests
{
	private const string ProblemJson = "application/problem+json";

	public class Matched
	{
		[Fact]
		public async Task Runs_Handler_And_Writes_Reply()
		{
			// Arrange
			var text = Rnd.Str;
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(_ => Reply.Text(text)));

			// Act
			var (response, body, _) = await host.SendAsync(HttpMethod.Get, "/");

			// Assert
			Assert.Equal(200, (int)response.StatusCode);
			Assert.Equal(text, body);
		}

		[Fact]
		public async Task Passes_Route_Values_To_Handler()
		{
			// Arrange
			var id = Rnd.Int;
			await using var host = await TestHost.StartAsync(r => r.Map("/posts/{id:int}").Get(req =>
				from value in req.Route<int>("id")
				from reply in Reply.Text(value.ToString())
				select reply
			));

			// Act
			var (_, body, _) = await host.SendAsync(HttpMethod.Get, $"/posts/{id}");

			// Assert
			Assert.Equal(id.ToString(), body);
		}

		[Fact]
		public async Task Passes_App_Json_Options_To_Handler()
		{
			// Arrange
			JsonSerializerOptions? options = null;
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(req =>
			{
				options = req.JsonOptions;
				return Reply.Text(Rnd.Str);
			}));

			// Act
			_ = await host.SendAsync(HttpMethod.Get, "/");

			// Assert
			Assert.Same(host.App.Json, options);
		}

		[Fact]
		public async Task Writes_Status_And_Headers()
		{
			// Arrange
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(_ =>
				from reply in Reply.Text(Rnd.Str)
				select reply with { StatusCode = 201 }
			));

			// Act
			var (response, _, _) = await host.SendAsync(HttpMethod.Get, "/");

			// Assert
			Assert.Equal(201, (int)response.StatusCode);
			Assert.Equal("Merlin", Assert.Single(response.Headers.Server).Product?.Name);
			Assert.Equal("GNU Terry Pratchett", Assert.Single(response.Headers.GetValues("X-Clacks-Overhead")));
		}

		[Fact]
		public async Task Writes_Json_With_App_Options()
		{
			// Arrange
			var dto = new TestDto(Rnd.Int, Rnd.Str);
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(_ => Reply.Json(dto)));

			// Act
			var (_, body, _) = await host.SendAsync(HttpMethod.Get, "/");

			// Assert
			Assert.Equal($$"""{"id":{{dto.Id}},"name":"{{dto.Name}}"}""", body);
		}

		[Fact]
		public async Task Unregistered_Json_Type_Is_Logged_Error_Reply()
		{
			// Arrange
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(_ => Reply.Json(new UnregisteredDto(Rnd.Int))));

			// Act
			var (response, _, done) = await host.SendAsync(HttpMethod.Get, "/");

			// Assert
			Assert.Equal(500, (int)response.StatusCode);
			Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
			Assert.Contains(host.Logs, l => l.Level == LogLevel.Error && l.Exception is NotSupportedException);
			Assert.Null(done.Exception);
		}
	}

	public class Unmatched
	{
		[Fact]
		public async Task Not_Found_Writes_404_Problem()
		{
			// Arrange
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(_ => Reply.Text(Rnd.Str)));

			// Act
			var (response, body, _) = await host.SendAsync(HttpMethod.Get, $"/{Rnd.Str}");

			// Assert
			Assert.Equal(404, (int)response.StatusCode);
			Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
			Assert.Equal(404, JsonElement.Parse(body).GetProperty("status").GetInt32());
		}

		[Fact]
		public async Task Method_Not_Allowed_Writes_405_Problem_With_Allow_Header()
		{
			// Arrange
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(_ => Reply.Text(Rnd.Str)).Put(_ => Reply.Text(Rnd.Str)));

			// Act
			var (response, body, _) = await host.SendAsync(HttpMethod.Post, "/");

			// Assert
			Assert.Equal(405, (int)response.StatusCode);
			Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
			Assert.Equal("GET, HEAD, PUT", string.Join(", ", response.Content.Headers.Allow));
			Assert.Equal(405, JsonElement.Parse(body).GetProperty("status").GetInt32());
		}

		[Fact]
		public async Task Not_Found_Is_Not_Logged_As_Error()
		{
			// Arrange
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(_ => Reply.Text(Rnd.Str)));

			// Act
			_ = await host.SendAsync(HttpMethod.Get, $"/{Rnd.Str}");

			// Assert
			Assert.DoesNotContain(host.Logs, l => l.Level >= LogLevel.Error);
		}
	}

	public class Head
	{
		[Fact]
		public async Task Get_Fallback_Writes_Headers_Without_Body()
		{
			// Arrange
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(_ => Reply.Text(Rnd.Str)));

			// Act
			var (response, _, done) = await host.SendAsync(HttpMethod.Head, "/");

			// Assert
			Assert.Equal(200, (int)response.StatusCode);
			Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
			Assert.Equal(0, done.BodyBytes);
		}

		[Fact]
		public async Task Explicit_Head_Writes_Headers_Without_Body()
		{
			// Arrange
			await using var host = await TestHost.StartAsync(r => r.Map("/").Head(_ => Reply.Text(Rnd.Str)));

			// Act
			var (response, _, done) = await host.SendAsync(HttpMethod.Head, "/");

			// Assert
			Assert.Equal(200, (int)response.StatusCode);
			Assert.Equal(0, done.BodyBytes);
		}

		[Fact]
		public async Task Not_Found_Writes_No_Body()
		{
			// Arrange
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(_ => Reply.Text(Rnd.Str)));

			// Act
			var (response, _, done) = await host.SendAsync(HttpMethod.Head, $"/{Rnd.Str}");

			// Assert
			Assert.Equal(404, (int)response.StatusCode);
			Assert.Equal(0, done.BodyBytes);
		}
	}

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
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(_ => R.Fail(Rnd.Str).Knd(KindOf(kind))));

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
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(_ => R.Fail(message).Knd(FailureKind.Invalid)));

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

	public class Cancellation
	{
		[Fact]
		public async Task Cancelled_Kind_Is_Not_Logged_As_Error_And_Writes_No_Reply()
		{
			// Arrange
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(_ => R.Fail(Rnd.Str).Knd(FailureKind.Cancelled)));

			// Act
			var (_, _, done) = await host.SendAsync(HttpMethod.Get, "/");

			// Assert
			Assert.DoesNotContain(host.Logs, l => l.Level >= LogLevel.Error);
			Assert.Equal(0, done.BodyBytes);
			Assert.Null(done.Exception);
		}

		[Fact]
		public async Task Aborted_Request_Is_Not_Logged_As_Error()
		{
			// Arrange
			var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(async req =>
			{
				entered.SetResult();
				try
				{
					await Task.Delay(Timeout.Infinite, req.Cancellation);
				}
				catch (OperationCanceledException e)
				{
					return R.Fail(e);
				}

				return Reply.Text(Rnd.Str);
			}));
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
			var send = host.Client.GetAsync("/", cts.Token);
			// if the handler never runs, the request completes on its own and the asserts fail rather than hang
			_ = await Task.WhenAny(entered.Task, send);

			// Act
			await cts.CancelAsync();
			_ = await Record.ExceptionAsync(() => send);
			var done = await host.Completed.ReadAsync(TestContext.Current.CancellationToken);

			// Assert
			Assert.True(done.Aborted);
			Assert.DoesNotContain(host.Logs, l => l.Level >= LogLevel.Error);
			Assert.Equal(0, done.BodyBytes);
			Assert.Null(done.Exception);
		}
	}
}
