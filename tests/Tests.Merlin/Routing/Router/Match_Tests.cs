// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Routing.Router_Tests;

public class Match_Tests
{
	private static Handler NewHandler() =>
		Substitute.For<Handler>();

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
			var result = router.Match("GET", $"/hello/{name}");

			// Assert
			var matched = Assert.IsType<Matched>(result);
			Assert.Same(handler, matched.Handler);
			Assert.Equal(name, Assert.IsType<string>(matched.Values["name"]));
		}

		[Fact]
		public void Captures_Typed_Param_As_Parsed_Value()
		{
			// Arrange
			var router = new Router();
			var id = Rnd.Int;
			router.Map("/posts/{id:int}").Get(NewHandler());

			// Act
			var result = router.Match("GET", $"/posts/{id}");

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
			var result = router.Match("GET", "/archive/2026/true");

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
			var result = router.Match("GET", "/posts/abc");

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
			var result = router.Match("GET", path);

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
			var result = router.Match("GET", "/a/b%2Fc");

			// Assert
			Assert.IsType<NotFound>(result);
		}
	}

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
	}

	public class Precedence
	{
		[Fact]
		public void Literal_Beats_Param()
		{
			// Arrange
			var router = new Router();
			var literal = NewHandler();
			var param = NewHandler();
			router.Map("/posts/{slug}").Get(param);
			router.Map("/posts/latest").Get(literal);

			// Act
			var latest = router.Match("GET", "/posts/latest");
			var other = router.Match("GET", "/posts/other");

			// Assert
			Assert.Same(literal, Assert.IsType<Matched>(latest).Handler);
			Assert.Same(param, Assert.IsType<Matched>(other).Handler);
		}

		[Fact]
		public void Typed_Param_Beats_String_Param()
		{
			// Arrange
			var router = new Router();
			var typed = NewHandler();
			var untyped = NewHandler();
			router.Map("/posts/{slug}").Get(untyped);
			router.Map("/posts/{id:int}").Get(typed);

			// Act
			var number = router.Match("GET", "/posts/42");
			var text = router.Match("GET", "/posts/hello");

			// Assert
			var n = Assert.IsType<Matched>(number);
			Assert.Same(typed, n.Handler);
			Assert.Equal(42, n.Values["id"]);
			var t = Assert.IsType<Matched>(text);
			Assert.Same(untyped, t.Handler);
			Assert.Equal("hello", t.Values["slug"]);
		}

		[Fact]
		public void Literal_And_Param_Beat_Catch_All()
		{
			// Arrange
			var router = new Router();
			var literal = NewHandler();
			var catchAll = NewHandler();
			router.Map("/files/{path:*}").Get(catchAll);
			router.Map("/files/readme").Get(literal);

			// Act
			var readme = router.Match("GET", "/files/readme");
			var other = router.Match("GET", "/files/other");

			// Assert
			Assert.Same(literal, Assert.IsType<Matched>(readme).Handler);
			Assert.Same(catchAll, Assert.IsType<Matched>(other).Handler);
		}

		[Fact]
		public void Backtracks_When_Literal_Branch_Dead_Ends()
		{
			// Arrange
			var router = new Router();
			var literal = NewHandler();
			var param = NewHandler();
			router.Map("/a/latest/x").Get(literal);
			router.Map("/a/{slug}/y").Get(param);

			// Act
			var result = router.Match("GET", "/a/latest/y");

			// Assert
			var matched = Assert.IsType<Matched>(result);
			Assert.Same(param, matched.Handler);
			Assert.Equal("latest", matched.Values["slug"]);
		}

		[Fact]
		public void Backtracks_To_Catch_All_When_Param_Branch_Dead_Ends()
		{
			// Arrange
			var router = new Router();
			var catchAll = NewHandler();
			router.Map("/files/{id:int}/info").Get(NewHandler());
			router.Map("/files/{path:*}").Get(catchAll);

			// Act
			var result = router.Match("GET", "/files/42/download");

			// Assert
			var matched = Assert.IsType<Matched>(result);
			Assert.Same(catchAll, matched.Handler);
			Assert.Equal("42/download", matched.Values["path"]);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void Equal_Priority_Tie_Goes_To_Param_Added_First(bool shortFirst)
		{
			// Arrange
			var router = new Router();
			router.Types.Add<short>("short", priority: 10);   // same priority as int, and "42" parses as both
			var shortHandler = NewHandler();
			var intHandler = NewHandler();
			if (shortFirst)
			{
				router.Map("/x/{s:short}").Get(shortHandler);
				router.Map("/x/{i:int}").Get(intHandler);
			}
			else
			{
				router.Map("/x/{i:int}").Get(intHandler);
				router.Map("/x/{s:short}").Get(shortHandler);
			}

			// Act
			var result = router.Match("GET", "/x/42");

			// Assert
			var matched = Assert.IsType<Matched>(result);
			Assert.Same(shortFirst ? shortHandler : intHandler, matched.Handler);
		}

		[Fact]
		public void Does_Not_Depend_On_Registration_Order()
		{
			// Arrange
			var first = new Router();
			var second = new Router();
			var literalA = NewHandler();
			var paramA = NewHandler();
			var literalB = NewHandler();
			var paramB = NewHandler();
			first.Map("/posts/latest").Get(literalA);
			first.Map("/posts/{slug}").Get(paramA);
			second.Map("/posts/{slug}").Get(paramB);
			second.Map("/posts/latest").Get(literalB);

			// Act
			var a = first.Match("GET", "/posts/latest");
			var b = second.Match("GET", "/posts/latest");

			// Assert
			Assert.Same(literalA, Assert.IsType<Matched>(a).Handler);
			Assert.Same(literalB, Assert.IsType<Matched>(b).Handler);
		}
	}

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

	public class Not_Found
	{
		[Fact]
		public void Returns_NotFound_When_No_Route_Matches()
		{
			// Arrange
			var router = new Router();
			router.Map("/posts").Get(NewHandler());

			// Act
			var result = router.Match("GET", "/other");

			// Assert
			Assert.IsType<NotFound>(result);
		}

		[Fact]
		public void Returns_NotFound_When_No_Routes_Are_Mapped()
		{
			// Arrange
			var router = new Router();

			// Act
			var result = router.Match("GET", "/posts");

			// Assert
			Assert.IsType<NotFound>(result);
		}

		[Fact]
		public void Returns_NotFound_When_Route_Has_No_Methods()
		{
			// Arrange
			var router = new Router();
			router.Map("/posts");

			// Act
			var result = router.Match("GET", "/posts");

			// Assert
			Assert.IsType<NotFound>(result);
		}
	}
}
