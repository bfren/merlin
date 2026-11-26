// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Routing.Router_Tests;

public partial class Match_Tests
{
	public class Literals
	{
		[Fact]
		public void Matches_Literal_Path()
		{
			// Arrange
			var router = new Router();
			var handler = NewHandler();
			router.Map("/posts").Get(handler);

			// Act
			var result = router.Match("GET", "/posts");

			// Assert
			var matched = Assert.IsType<Matched>(result);
			Assert.Same(handler, matched.Handler);
			Assert.Empty(matched.Values);
		}

		[Theory]
		[InlineData("/")]
		[InlineData("")]
		public void Matches_Root(string path)
		{
			// Arrange
			var router = new Router();
			var handler = NewHandler();
			router.Map("/").Get(handler);

			// Act
			var result = router.Match("GET", path);

			// Assert
			var matched = Assert.IsType<Matched>(result);
			Assert.Same(handler, matched.Handler);
		}

		[Fact]
		public void Ignores_Case()
		{
			// Arrange
			var router = new Router();
			var handler = NewHandler();
			router.Map("/posts").Get(handler);

			// Act
			var result = router.Match("GET", "/POSTS");

			// Assert
			var matched = Assert.IsType<Matched>(result);
			Assert.Same(handler, matched.Handler);
		}

		[Fact]
		public void Ignores_Trailing_Slash()
		{
			// Arrange
			var router = new Router();
			var handler = NewHandler();
			router.Map("/posts").Get(handler);

			// Act
			var result = router.Match("GET", "/posts/");

			// Assert
			var matched = Assert.IsType<Matched>(result);
			Assert.Same(handler, matched.Handler);
		}

		[Theory]
		[InlineData("//posts/comments")]
		[InlineData("/posts//comments")]
		[InlineData("/posts/comments//")]
		[InlineData("//posts//comments//")]
		public void Ignores_Empty_Segments(string path)
		{
			// Arrange
			var router = new Router();
			var handler = NewHandler();
			router.Map("/posts/comments").Get(handler);

			// Act
			var result = router.Match("GET", path);

			// Assert
			var matched = Assert.IsType<Matched>(result);
			Assert.Same(handler, matched.Handler);
		}

		[Fact]
		public void Does_Not_Match_Longer_Path()
		{
			// Arrange
			var router = new Router();
			router.Map("/posts").Get(NewHandler());

			// Act
			var result = router.Match("GET", "/posts/extra");

			// Assert
			Assert.IsType<NotFound>(result);
		}

		[Fact]
		public void Does_Not_Match_Shorter_Path()
		{
			// Arrange
			var router = new Router();
			router.Map("/posts/latest").Get(NewHandler());

			// Act
			var result = router.Match("GET", "/posts");

			// Assert
			Assert.IsType<NotFound>(result);
		}
	}
}
