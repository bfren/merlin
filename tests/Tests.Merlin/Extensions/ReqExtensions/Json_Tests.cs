// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text.Json;
using Wrap.Testing;
using static Merlin.Extensions.ReqExtensions_Tests.Helpers;

namespace Merlin.Extensions.ReqExtensions_Tests;

public class Json_Tests
{
	private const string Json = "application/json";

	public class With_Info
	{
		[Fact]
		public async Task Returns_Deserialised_Value()
		{
			// Arrange
			var title = Rnd.Str;
			var views = Rnd.Int;
			var author = Rnd.Str;
			var req = NewReq(Json, $$"""
				{"title":"{{title}}","views":{{views}},"draft":true,"tags":["a","b"],"author_name":"{{author}}"}
				""");

			// Act
			var result = await req.Json(BinderJson.Default.PostDto);

			// Assert
			var post = result.AssertOk();
			Assert.Equal(title, post.Title);
			Assert.Equal(views, post.Views);
			Assert.True(post.Draft);
			Assert.Equal(["a", "b"], post.Tags);
			Assert.Equal(author, post.AuthorName);
		}

		[Fact]
		public async Task Binds_Init_Only_Properties()
		{
			// Arrange
			var title = Rnd.Str;
			var req = NewReq(Json, $$"""{"title":"{{title}}","views":3}""");

			// Act
			var result = await req.Json(BinderJson.Default.PostInitDto);

			// Assert
			var post = result.AssertOk();
			Assert.Equal(title, post.Title);
			Assert.Equal(3, post.Views);
		}

		[Theory]
		[InlineData("application/json; charset=utf-8")]
		[InlineData("APPLICATION/JSON")]
		[InlineData("application/problem+json")]
		public async Task Accepts_Json_Content_Types(string contentType)
		{
			// Arrange
			var req = NewReq(contentType, """{"title":"x"}""");

			// Act
			var result = await req.Json(BinderJson.Default.PostDto);

			// Assert
			Assert.Equal("x", result.AssertOk().Title);
		}

		[Fact]
		public async Task Ignores_Unknown_Properties()
		{
			// Arrange
			var req = NewReq(Json, """{"title":"x","other":1}""");

			// Act
			var result = await req.Json(BinderJson.Default.PostDto);

			// Assert
			Assert.Equal("x", result.AssertOk().Title);
		}
	}

	public class Failures
	{
		[Theory]
		[InlineData("text/plain")]
		[InlineData("application/x-www-form-urlencoded")]
		[InlineData(null)]
		public async Task Wrong_Content_Type_Returns_Failure(string? contentType)
		{
			// Arrange
			var req = NewReq(contentType, """{"title":"x"}""");

			// Act
			var result = await req.Json(BinderJson.Default.PostDto);

			// Assert
			result.AssertFailure();
		}

		[Theory]
		[InlineData("")]
		[InlineData("null")]
		public async Task Empty_Body_Returns_Failure(string body)
		{
			// Arrange
			var req = NewReq(Json, body);

			// Act
			var result = await req.Json(BinderJson.Default.PostDto);

			// Assert
			result.AssertFailure();
		}

		[Theory]
		[InlineData("{")]
		[InlineData("not json")]
		[InlineData("""{"title":}""")]
		public async Task Invalid_Json_Returns_Failure(string body)
		{
			// Arrange
			var req = NewReq(Json, body);

			// Act
			var result = await req.Json(BinderJson.Default.PostDto);

			// Assert
			result.AssertFailure();
		}

		[Theory]
		[InlineData("""{"views":"abc"}""")]
		[InlineData("""{"views":"3"}""")]
		[InlineData("""{"draft":"yes"}""")]
		[InlineData("""[1,2]""")]
		public async Task Wrong_Shape_Returns_Failure(string body)
		{
			// Arrange
			var req = NewReq(Json, body);

			// Act
			var result = await req.Json(BinderJson.Default.PostDto);

			// Assert
			result.AssertFailure();
		}
	}

	public class Without_Info
	{
		[Fact]
		public async Task Resolves_Type_From_Request_Options()
		{
			// Arrange
			var title = Rnd.Str;
			var req = NewReq(Json, $$"""{"title":"{{title}}"}""");

			// Act
			var result = await req.Json<PostDto>();

			// Assert
			Assert.Equal(title, result.AssertOk().Title);
		}

		[Fact]
		public async Task Uses_Request_Options()
		{
			// Arrange
			var options = new JsonSerializerOptions
			{
				TypeInfoResolver = BinderJson.Default,
				NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
			};
			var req = NewReq(Json, """{"views":"3"}""", options);

			// Act
			var result = await req.Json<PostDto>();

			// Assert
			Assert.Equal(3, result.AssertOk().Views);
		}

		[Fact]
		public async Task Unregistered_Type_Returns_Failure()
		{
			// Arrange
			var req = NewReq(Json, """{"value":"x"}""");

			// Act
			var result = await req.Json<Unregistered>();

			// Assert
			result.AssertFailure();
		}

		[Fact]
		public async Task Wrong_Content_Type_Returns_Failure()
		{
			// Arrange
			var req = NewReq("text/plain", """{"title":"x"}""");

			// Act
			var result = await req.Json<PostDto>();

			// Assert
			result.AssertFailure();
		}

		[Fact]
		public async Task Invalid_Json_Returns_Failure()
		{
			// Arrange
			var req = NewReq(Json, "{");

			// Act
			var result = await req.Json<PostDto>();

			// Assert
			result.AssertFailure();
		}
	}
}
