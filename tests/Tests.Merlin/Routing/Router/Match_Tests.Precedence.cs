// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Merlin.Routing.Router_Tests;

public partial class Match_Tests
{
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

			router.Lock(Sp, new ServiceCollection());

			// Act
			var latest = router.Match(Method.Get, "/posts/latest");
			var other = router.Match(Method.Get, "/posts/other");

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

			router.Lock(Sp, new ServiceCollection());

			// Act
			var number = router.Match(Method.Get, "/posts/42");
			var text = router.Match(Method.Get, "/posts/hello");

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

			router.Lock(Sp, new ServiceCollection());

			// Act
			var readme = router.Match(Method.Get, "/files/readme");
			var other = router.Match(Method.Get, "/files/other");

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

			router.Lock(Sp, new ServiceCollection());

			// Act
			var result = router.Match(Method.Get, "/a/latest/y");

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

			router.Lock(Sp, new ServiceCollection());

			// Act
			var result = router.Match(Method.Get, "/files/42/download");

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

			router.Lock(Sp, new ServiceCollection());

			// Act
			var result = router.Match(Method.Get, "/x/42");

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

			first.Lock(Sp, new ServiceCollection());
			second.Lock(Sp, new ServiceCollection());

			// Act
			var a = first.Match(Method.Get, "/posts/latest");
			var b = second.Match(Method.Get, "/posts/latest");

			// Assert
			Assert.Same(literalA, Assert.IsType<Matched>(a).Handler);
			Assert.Same(literalB, Assert.IsType<Matched>(b).Handler);
		}
	}
}
