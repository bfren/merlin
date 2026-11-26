// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text.Json;
using Merlin.Extensions;
using Merlin.Replies;
using Microsoft.Extensions.Logging;
using Wrap.Linq;

namespace Merlin.App_Tests;

public partial class StartAsync_Tests
{
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
}
