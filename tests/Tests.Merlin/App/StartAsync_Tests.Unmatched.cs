// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text.Json;
using Merlin.Replies;
using Microsoft.Extensions.Logging;

namespace Merlin.App_Tests;

public partial class StartAsync_Tests
{
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
}
