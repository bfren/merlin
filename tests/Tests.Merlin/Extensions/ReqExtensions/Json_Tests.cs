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
			Assert.Equal(["a", "b"], post.Tags ?? []);
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
			var req = NewReq(contentType, /*lang=json,strict*/ """{"title":"x"}""");

			// Act
			var result = await req.Json(BinderJson.Default.PostDto);

			// Assert
			Assert.Equal("x", result.AssertOk().Title);
		}

		[Fact]
		public async Task Ignores_Unknown_Properties()
		{
			// Arrange
			var req = NewReq(Json, /*lang=json,strict*/ """{"title":"x","other":1}""");

			// Act
			var result = await req.Json(BinderJson.Default.PostDto);

			// Assert
			Assert.Equal("x", result.AssertOk().Title);
		}
	}

	private const string NotJson = "Request body is not JSON.";

	private const string Empty = "Request body is empty.";

	private const string InvalidJson = "Request body is not valid JSON at {Path}.";

	private const string Unregistered = "Type {Type} is not registered for JSON.";

	private static void AssertFailure<T>(Wrap.Result<T> result, Wrap.FailureKind kind, string message, params object[] args)
	{
		var failure = result.AssertFailure(message, args);
		Assert.Equal(kind, failure.Kind);
	}

	public class Failures
	{
		[Theory]
		[InlineData("text/plain")]
		[InlineData("application/x-www-form-urlencoded")]
		[InlineData(null)]
		public async Task Wrong_Content_Type_Is_Invalid(string? contentType)
		{
			// Arrange
			var req = NewReq(contentType, /*lang=json,strict*/ """{"title":"x"}""");

			// Act
			var result = await req.Json(BinderJson.Default.PostDto);

			// Assert
			AssertFailure(result, Wrap.FailureKind.Invalid, NotJson);
		}

		[Theory]
		[InlineData("")]
		[InlineData("null")]
		public async Task Empty_Body_Is_Invalid(string body)
		{
			// Arrange
			var req = NewReq(Json, body);

			// Act
			var result = await req.Json(BinderJson.Default.PostDto);

			// Assert
			AssertFailure(result, Wrap.FailureKind.Invalid, Empty);
		}

		// paths checked against System.Text.Json on 2026-11-26
		[Theory]
		[InlineData("{", "$")]
		[InlineData("not json", "$")]
		[InlineData("""{"title":}""", "$.title")]
		public async Task Invalid_Json_Is_Invalid_With_Path(string body, string path)
		{
			// Arrange
			var req = NewReq(Json, body);

			// Act
			var result = await req.Json(BinderJson.Default.PostDto);

			// Assert
			AssertFailure(result, Wrap.FailureKind.Invalid, InvalidJson, path);
		}

		[Theory]
		[InlineData(/*lang=json,strict*/ """{"views":"abc"}""", "$.views")]
		[InlineData(/*lang=json,strict*/ """{"views":"3"}""", "$.views")]
		[InlineData(/*lang=json,strict*/ """{"draft":"yes"}""", "$.draft")]
		[InlineData("""[1,2]""", "$")]
		public async Task Wrong_Shape_Is_Invalid_With_Path(string body, string path)
		{
			// Arrange
			var req = NewReq(Json, body);

			// Act
			var result = await req.Json(BinderJson.Default.PostDto);

			// Assert
			AssertFailure(result, Wrap.FailureKind.Invalid, InvalidJson, path);
		}

		[Fact]
		public async Task Cancelled_Read_Has_Kind_Cancelled()
		{
			// Arrange
			var req = NewReq(Json, /*lang=json,strict*/ """{"title":"x"}""", cancelled: true);

			// Act
			var result = await req.Json(BinderJson.Default.PostDto);

			// Assert
			Assert.Equal(Wrap.FailureKind.Cancelled, result.AssertFailure().Kind);
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
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
			};
			var req = NewReq(Json, /*lang=json,strict*/ """{"views":"3"}""", options);

			// Act
			var result = await req.Json<PostDto>();

			// Assert
			Assert.Equal(3, result.AssertOk().Views);
		}

		[Fact]
		public async Task Unregistered_Type_Is_Unexpected()
		{
			// Arrange
			var req = NewReq(Json, /*lang=json,strict*/ """{"value":"x"}""");

			// Act
			var result = await req.Json<Unregistered>();

			// Assert
			// the type was never registered: the server's fault, not the client's
			AssertFailure(result, Wrap.FailureKind.Unexpected, Unregistered, typeof(Unregistered));
		}

		[Fact]
		public async Task Wrong_Content_Type_Is_Invalid()
		{
			// Arrange
			var req = NewReq("text/plain", /*lang=json,strict*/ """{"title":"x"}""");

			// Act
			var result = await req.Json<PostDto>();

			// Assert
			AssertFailure(result, Wrap.FailureKind.Invalid, NotJson);
		}

		[Fact]
		public async Task Invalid_Json_Is_Invalid_With_Path()
		{
			// Arrange
			var req = NewReq(Json, "{");

			// Act
			var result = await req.Json<PostDto>();

			// Assert
			AssertFailure(result, Wrap.FailureKind.Invalid, InvalidJson, "$");
		}

		[Fact]
		public async Task Cancelled_Read_Has_Kind_Cancelled()
		{
			// Arrange
			var req = NewReq(Json, /*lang=json,strict*/ """{"title":"x"}""", cancelled: true);

			// Act
			var result = await req.Json<PostDto>();

			// Assert
			Assert.Equal(Wrap.FailureKind.Cancelled, result.AssertFailure().Kind);
		}
	}
}
