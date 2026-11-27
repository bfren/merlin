// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Domain;

namespace Merlin.Routing.Router_Tests;

public partial class Match_Tests
{
	public class Params
	{
		[Fact]
		public void Captures_Untyped_Param_As_String()
		{
			// Arrange
			var router = new Router();
			var handler = NewHandler();
			var name = Rnd.Str;
			router.Map("/hello/{name}").Get(handler);

			// Act
			var result = router.Match(Method.Get, $"/hello/{name}");

			// Assert
			var matched = Assert.IsType<Matched>(result);
			Assert.Same(handler, matched.Handler);
			Assert.Equal(name, Assert.IsType<string>(matched.Values["name"]));
		}

		[Theory]
		[InlineData("NAME")]
		[InlineData("Name")]
		public void Values_Are_Found_By_Name_Ignoring_Case(string key)
		{
			// Arrange
			var router = new Router();
			var name = Rnd.Str;
			router.Map("/hello/{name}").Get(NewHandler());

			// Act
			var result = router.Match(Method.Get, $"/hello/{name}");

			// Assert
			var matched = Assert.IsType<Matched>(result);
			Assert.Equal(name, matched.Values[key]);
		}

		[Fact]
		public void Captures_Typed_Param_As_Parsed_Value()
		{
			// Arrange
			var router = new Router();
			var id = Rnd.Int;
			router.Map("/posts/{id:int}").Get(NewHandler());

			// Act
			var result = router.Match(Method.Get, $"/posts/{id}");

			// Assert
			var matched = Assert.IsType<Matched>(result);
			Assert.Equal(id, Assert.IsType<int>(matched.Values["id"]));
		}

		[Fact]
		public void Captures_Multiple_Params()
		{
			// Arrange
			var router = new Router();
			router.Map("/archive/{year:int}/{draft:bool}").Get(NewHandler());

			// Act
			var result = router.Match(Method.Get, "/archive/2026/true");

			// Assert
			var matched = Assert.IsType<Matched>(result);
			Assert.Equal(2026, Assert.IsType<int>(matched.Values["year"]));
			Assert.True(Assert.IsType<bool>(matched.Values["draft"]));
		}

		[Fact]
		public void Does_Not_Match_Value_That_Cannot_Be_Parsed()
		{
			// Arrange
			var router = new Router();
			router.Map("/posts/{id:int}").Get(NewHandler());

			// Act
			var result = router.Match(Method.Get, "/posts/abc");

			// Assert
			Assert.IsType<NotFound>(result);
		}

		// Kestrel decodes the path before Match sees it, except %2F, so Match does not decode again

		[Theory]
		[InlineData("/hello/Ben Green", "Ben Green")]
		[InlineData("/hello/100%25", "100%25")]
		[InlineData("/hello/a%2Fb", "a%2Fb")]
		public void Does_Not_Decode_Value(string path, string expected)
		{
			// Arrange
			var router = new Router();
			router.Map("/hello/{name}").Get(NewHandler());

			// Act
			var result = router.Match(Method.Get, path);

			// Assert
			var matched = Assert.IsType<Matched>(result);
			Assert.Equal(expected, matched.Values["name"]);
		}

		[Fact]
		public void Encoded_Slash_Does_Not_Split_Segment()
		{
			// Arrange
			var router = new Router();
			router.Map("/a/{x}/{y}").Get(NewHandler());

			// Act
			var result = router.Match(Method.Get, "/a/b%2Fc");

			// Assert
			Assert.IsType<NotFound>(result);
		}
	}
}
