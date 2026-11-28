// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Domain;
using Microsoft.Extensions.DependencyInjection;

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

			router.Lock(Sp, new ServiceCollection());

			// Act
			var result = router.Match(Method.Get, "/posts");

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

			router.Lock(Sp, new ServiceCollection());

			// Act
			var result = router.Match(Method.Get, path);

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

			router.Lock(Sp, new ServiceCollection());

			// Act
			var result = router.Match(Method.Get, "/POSTS");

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

			router.Lock(Sp, new ServiceCollection());

			// Act
			var result = router.Match(Method.Get, "/posts/");

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

			router.Lock(Sp, new ServiceCollection());

			// Act
			var result = router.Match(Method.Get, path);

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

			router.Lock(Sp, new ServiceCollection());

			// Act
			var result = router.Match(Method.Get, "/posts/extra");

			// Assert
			Assert.IsType<NotFound>(result);
		}

		[Fact]
		public void Does_Not_Match_Shorter_Path()
		{
			// Arrange
			var router = new Router();
			router.Map("/posts/latest").Get(NewHandler());

			router.Lock(Sp, new ServiceCollection());

			// Act
			var result = router.Match(Method.Get, "/posts");

			// Assert
			Assert.IsType<NotFound>(result);
		}
	}
}
