// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Domain;
using Merlin.Replies;

namespace Merlin.App_Tests;

public partial class StartAsync_Tests
{
	public class Head
	{
		[Fact]
		public async Task Get_Fallback_Writes_Headers_Without_Body()
		{
			// Arrange
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(_ => Reply.Text(Rnd.Str)));

			// Act
			var (response, _, done) = await host.SendAsync(Method.Head, "/");

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
			var (response, _, done) = await host.SendAsync(Method.Head, "/");

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
			var (response, _, done) = await host.SendAsync(Method.Head, $"/{Rnd.Str}");

			// Assert
			Assert.Equal(404, (int)response.StatusCode);
			Assert.Equal(0, done.BodyBytes);
		}
	}
}
