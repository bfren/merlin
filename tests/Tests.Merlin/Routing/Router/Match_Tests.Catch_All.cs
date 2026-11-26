// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Routing.Router_Tests;

public partial class Match_Tests
{
	public class Catch_All
	{
		[Fact]
		public void Captures_Rest_Of_Path()
		{
			// Arrange
			var router = new Router();
			var handler = NewHandler();
			router.Map("/files/{path:*}").Get(handler);

			// Act
			var result = router.Match("GET", "/files/docs/2026/report.pdf");

			// Assert
			var matched = Assert.IsType<Matched>(result);
			Assert.Same(handler, matched.Handler);
			Assert.Equal("docs/2026/report.pdf", matched.Values["path"]);
		}

		[Fact]
		public void Captures_Single_Segment()
		{
			// Arrange
			var router = new Router();
			router.Map("/files/{path:*}").Get(NewHandler());

			// Act
			var result = router.Match("GET", "/files/readme.txt");

			// Assert
			var matched = Assert.IsType<Matched>(result);
			Assert.Equal("readme.txt", matched.Values["path"]);
		}

		[Fact]
		public void Keeps_Encoded_Slash()
		{
			// Arrange
			var router = new Router();
			router.Map("/files/{path:*}").Get(NewHandler());

			// Act
			var result = router.Match("GET", "/files/..%2F..%2Fsecret");

			// Assert
			var matched = Assert.IsType<Matched>(result);
			Assert.Equal("..%2F..%2Fsecret", matched.Values["path"]);
		}

		[Fact]
		public void Requires_At_Least_One_Segment()
		{
			// Arrange
			var router = new Router();
			router.Map("/files/{path:*}").Get(NewHandler());

			// Act
			var result = router.Match("GET", "/files");

			// Assert
			Assert.IsType<NotFound>(result);
		}

		[Theory]
		[InlineData("/")]
		[InlineData("")]
		public void Root_Catch_All_Does_Not_Match_Root(string path)
		{
			// Arrange
			var router = new Router();
			router.Map("/{path:*}").Get(NewHandler());

			// Act
			var result = router.Match("GET", path);

			// Assert
			Assert.IsType<NotFound>(result);
		}

		[Fact]
		public void Root_Catch_All_Matches_Any_Other_Path()
		{
			// Arrange
			var router = new Router();
			var handler = NewHandler();
			var (first, second) = (Rnd.Str, Rnd.Str);
			router.Map("/{path:*}").Get(handler);

			// Act
			var result = router.Match("GET", $"/{first}/{second}");

			// Assert
			var matched = Assert.IsType<Matched>(result);
			Assert.Same(handler, matched.Handler);
			Assert.Equal($"{first}/{second}", matched.Values["path"]);
		}

		[Fact]
		public void Root_Route_Can_Sit_Beside_Root_Catch_All()
		{
			// Arrange
			var router = new Router();
			var root = NewHandler();
			router.Map("/").Get(root);
			router.Map("/{path:*}").Get(NewHandler());

			// Act
			var result = router.Match("GET", "/");

			// Assert
			var matched = Assert.IsType<Matched>(result);
			Assert.Same(root, matched.Handler);
		}
	}
}
