// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Routing.Router_Tests;

public partial class Match_Tests
{
	public class Methods
	{
		[Fact]
		public void Matches_Handler_For_Method()
		{
			// Arrange
			var router = new Router();
			var get = NewHandler();
			var post = NewHandler();
			router.Map("/posts").Get(get).Post(post);

			// Act
			var getResult = router.Match("GET", "/posts");
			var postResult = router.Match("POST", "/posts");

			// Assert
			Assert.Same(get, Assert.IsType<Matched>(getResult).Handler);
			Assert.Same(post, Assert.IsType<Matched>(postResult).Handler);
		}

		[Fact]
		public void Ignores_Method_Case()
		{
			// Arrange
			var router = new Router();
			var handler = NewHandler();
			router.Map("/posts").Get(handler);

			// Act
			var result = router.Match("get", "/posts");

			// Assert
			Assert.Same(handler, Assert.IsType<Matched>(result).Handler);
		}

		[Fact]
		public void Head_Falls_Back_To_Get()
		{
			// Arrange
			var router = new Router();
			var handler = NewHandler();
			router.Map("/posts").Get(handler);

			// Act
			var result = router.Match("HEAD", "/posts");

			// Assert
			Assert.Same(handler, Assert.IsType<Matched>(result).Handler);
		}

		[Fact]
		public void Head_Falls_Back_To_Get_With_Params()
		{
			// Arrange
			var router = new Router();
			var handler = NewHandler();
			router.Map("/posts/{id:int}").Get(handler);

			// Act
			var result = router.Match("HEAD", "/posts/42");

			// Assert
			var matched = Assert.IsType<Matched>(result);
			Assert.Same(handler, matched.Handler);
			Assert.Equal(42, matched.Values["id"]);
		}

		[Fact]
		public void Explicit_Head_Beats_Get_Fallback()
		{
			// Arrange
			var router = new Router();
			var get = NewHandler();
			var head = NewHandler();
			router.Map("/posts").Get(get);
			router.Map("/posts").Head(head);

			// Act
			var headResult = router.Match("HEAD", "/posts");
			var getResult = router.Match("GET", "/posts");

			// Assert
			Assert.Same(head, Assert.IsType<Matched>(headResult).Handler);
			Assert.Same(get, Assert.IsType<Matched>(getResult).Handler);
		}

		[Fact]
		public void Head_Without_Get_Returns_MethodNotAllowed()
		{
			// Arrange
			var router = new Router();
			router.Map("/posts").Post(NewHandler());

			// Act
			var result = router.Match("HEAD", "/posts");

			// Assert
			var notAllowed = Assert.IsType<MethodNotAllowed>(result);
			Assert.Equal(["POST"], notAllowed.Allowed);
		}

		[Fact]
		public void Head_Does_Not_Make_Get_Allowed()
		{
			// Arrange
			var router = new Router();
			router.Map("/posts").Head(NewHandler());

			// Act
			var result = router.Match("GET", "/posts");

			// Assert
			var notAllowed = Assert.IsType<MethodNotAllowed>(result);
			Assert.Equal(["HEAD"], notAllowed.Allowed);
		}

		[Fact]
		public void Returns_MethodNotAllowed_With_Allowed_Methods()
		{
			// Arrange
			var router = new Router();
			router.Map("/posts").Get(NewHandler()).Put(NewHandler());

			// Act
			var result = router.Match("DELETE", "/posts");

			// Assert
			var notAllowed = Assert.IsType<MethodNotAllowed>(result);
			Assert.Equal(["GET", "HEAD", "PUT"], notAllowed.Allowed.Order());
		}

		[Fact]
		public void Allowed_Methods_List_Head_Once_When_Mapped_Explicitly()
		{
			// Arrange
			var router = new Router();
			router.Map("/posts").Get(NewHandler());
			router.Map("/posts").Head(NewHandler());

			// Act
			var result = router.Match("DELETE", "/posts");

			// Assert
			var notAllowed = Assert.IsType<MethodNotAllowed>(result);
			Assert.Equal(["GET", "HEAD"], notAllowed.Allowed.Order());
		}

		[Fact]
		public void Allowed_Methods_Only_Include_Methods_Matching_Path()
		{
			// Arrange
			var router = new Router();
			router.Map("/posts").Get(NewHandler());
			router.Map("/other").Put(NewHandler());

			// Act
			var result = router.Match("POST", "/posts");

			// Assert
			var notAllowed = Assert.IsType<MethodNotAllowed>(result);
			Assert.Equal(["GET", "HEAD"], notAllowed.Allowed.Order());
		}
	}
}
