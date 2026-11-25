// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Routing.Router_Tests;

public class Add_Tests
{
	private static Exception? Add(Router router, string method, string source) =>
		Record.Exception(() => router.Add(method, Template.Parse(source, router.Types), Substitute.For<Handler>()));

	public class Duplicates
	{
		[Theory]
		[InlineData("/posts", "/posts")]
		[InlineData("/posts", "/POSTS/")]
		[InlineData("/", "")]
		[InlineData("/posts/{id:int}", "/posts/{id:int}")]
		[InlineData("/posts/{slug}", "/posts/{slug:string}")]
		public void Same_Route_And_Method_Throws(string first, string second)
		{
			// Arrange
			var router = new Router();
			Add(router, "GET", first);

			// Act
			var result = Add(router, "GET", second);

			// Assert
			var ex = Assert.IsType<InvalidOperationException>(result);
			Assert.Contains("already mapped", ex.Message);
		}

		[Fact]
		public void Method_Is_Case_Insensitive()
		{
			// Arrange
			var router = new Router();
			Add(router, "GET", "/posts");

			// Act
			var result = Add(router, "get", "/posts");

			// Assert
			Assert.IsType<InvalidOperationException>(result);
		}

		[Fact]
		public void Same_Route_With_Different_Methods_Is_Allowed()
		{
			// Arrange
			var router = new Router();
			Add(router, "GET", "/posts");

			// Act
			var result = Add(router, "POST", "/posts");

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void Second_Catch_All_At_Same_Point_Throws()
		{
			// Arrange
			var router = new Router();
			Add(router, "GET", "/files/{path:*}");

			// Act
			var result = Add(router, "GET", "/files/{rest:*}");

			// Assert
			var ex = Assert.IsType<InvalidOperationException>(result);
			Assert.Contains("catch-all already mapped", ex.Message);
		}

		[Fact]
		public void Catch_All_With_Different_Methods_Is_Allowed()
		{
			// Arrange
			var router = new Router();
			Add(router, "GET", "/files/{path:*}");

			// Act
			var result = Add(router, "PUT", "/files/{path:*}");

			// Assert
			Assert.Null(result);
		}
	}

	public class Overlapping
	{
		[Theory]
		[InlineData("/posts", "/posts/latest")]
		[InlineData("/posts/latest", "/posts")]
		[InlineData("/posts/{id:int}", "/posts/{id:int}/edit")]
		[InlineData("/posts/{id:int}", "/posts/{slug}")]
		[InlineData("/posts/{id:int}", "/posts/{id:long}")]
		[InlineData("/posts/{slug}", "/posts/latest")]
		[InlineData("/files", "/files/{path:*}")]
		[InlineData("/files/{path:*}", "/files/readme")]
		[InlineData("/files/{path:*}", "/files/{id:int}")]
		public void Different_Routes_Are_Allowed(string first, string second)
		{
			// Arrange
			var router = new Router();
			Add(router, "GET", first);

			// Act
			var result = Add(router, "GET", second);

			// Assert
			Assert.Null(result);
		}
	}

	public class Param_Conflicts
	{
		[Theory]
		[InlineData("/posts/{id:int}", "/posts/{num:int}/edit")]
		[InlineData("/posts/{slug}", "/posts/{title:string}/edit")]
		[InlineData("/posts/{id:int}", "/posts/{ID:int}/edit")]
		public void Same_Type_With_Different_Name_Throws(string first, string second)
		{
			// Arrange
			var router = new Router();
			Add(router, "GET", first);

			// Act
			var result = Add(router, "GET", second);

			// Assert
			var ex = Assert.IsType<InvalidOperationException>(result);
			Assert.Contains("conflicts with", ex.Message);
		}

		[Fact]
		public void Different_Methods_Do_Not_Conflict()
		{
			// Arrange
			var router = new Router();
			Add(router, "GET", "/posts/{id:int}");

			// Act
			var result = Add(router, "POST", "/posts/{num:int}");

			// Assert
			Assert.Null(result);
		}
	}

	[Fact]
	public void Throws_When_Locked()
	{
		// Arrange
		var router = new Router();
		router.Lock();

		// Act
		var result = Add(router, "GET", "/posts");

		// Assert
		var ex = Assert.IsType<InvalidOperationException>(result);
		Assert.Contains("locked", ex.Message);
	}
}
