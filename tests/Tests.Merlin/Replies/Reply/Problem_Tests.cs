// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text.Json;
using Wrap.Testing;
using static Merlin.Replies.Helpers;

namespace Merlin.Replies.Reply_Tests;

public class Problem_Tests
{
	private static Problem NewProblem(int status = 404) =>
		new() { Title = Rnd.Str, Status = status, Type = Rnd.Str, Detail = Rnd.Str, Instance = Rnd.Str };

	public class Reply_Shape
	{
		[Fact]
		public void Returns_JsonReply_With_Problem()
		{
			// Arrange
			var problem = NewProblem();

			// Act
			var result = Reply.Problem(problem);

			// Assert
			var reply = Assert.IsType<JsonReply<Problem>>(result.AssertOk());
			Assert.Same(problem, reply.Content);
		}

		[Theory]
		[InlineData(400)]
		[InlineData(404)]
		[InlineData(500)]
		public void Sets_Status_From_Problem(int status)
		{
			// Arrange
			var problem = NewProblem(status);

			// Act
			var result = Reply.Problem(problem);

			// Assert
			Assert.Equal(status, result.AssertOk().StatusCode);
		}

		[Fact]
		public void Sets_Problem_Content_Type()
		{
			// Arrange
			var problem = NewProblem();

			// Act
			var result = Reply.Problem(problem);

			// Assert
			Assert.Equal("application/problem+json", result.AssertOk().Headers["Content-Type"]);
		}

		[Fact]
		public void Keeps_Default_Headers()
		{
			// Arrange
			var problem = NewProblem();

			// Act
			var result = Reply.Problem(problem);

			// Assert
			var headers = result.AssertOk().Headers;
			Assert.Equal("Merlin", headers["Server"]);
			Assert.Equal("GNU Terry Pratchett", headers["X-Clacks-Overhead"]);
		}
	}

	public class Written
	{
		[Fact]
		public async Task Writes_Status_And_Content_Type()
		{
			// Arrange
			var reply = Reply.Problem(NewProblem(404)).AssertOk();

			// Act
			var (_, response, _) = await WriteAsync(reply);

			// Assert
			Assert.Equal(404, response.StatusCode);
			Assert.Equal("application/problem+json", response.ContentType);
		}

		[Fact]
		public async Task Writes_Every_Member_With_RFC_9457_Names()
		{
			// Arrange
			var problem = NewProblem(400);
			var reply = Reply.Problem(problem).AssertOk();

			// Act
			var (_, _, body) = await WriteAsync(reply);

			// Assert
			using var json = JsonDocument.Parse(body);
			var root = json.RootElement;
			Assert.Equal(problem.Type, root.GetProperty("type").GetString());
			Assert.Equal(problem.Title, root.GetProperty("title").GetString());
			Assert.Equal(400, root.GetProperty("status").GetInt32());
			Assert.Equal(problem.Detail, root.GetProperty("detail").GetString());
			Assert.Equal(problem.Instance, root.GetProperty("instance").GetString());
			Assert.Equal(5, root.EnumerateObject().Count());
		}

		[Fact]
		public async Task Leaves_Out_Members_That_Are_Not_Set()
		{
			// Arrange
			var reply = Reply.Problem(new() { Title = "Not Found", Status = 404 }).AssertOk();

			// Act
			var (_, _, body) = await WriteAsync(reply);

			// Assert
			// an absent "type" means "about:blank" (RFC 9457 section 3.1.1)
			Assert.Equal(/*lang=json,strict*/ """{"title":"Not Found","status":404}""", body);
		}
	}
}
