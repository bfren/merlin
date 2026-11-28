// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Domain;
using Merlin.Replies;
using Wrap;
using Wrap.Testing;

namespace Merlin.Routing.Router_Tests;

public class Lock_Tests
{
	private static IServiceProvider NewProvider() =>
		Substitute.For<IServiceProvider>();

	// a factory returning a new handler, so the handler's origin can be checked
	private static (HandlerFactory Factory, HandlerAsync Handler) NewFactory()
	{
		var handler = Substitute.For<HandlerAsync>();
		var factory = Substitute.For<HandlerFactory>();
		_ = factory(Arg.Any<IServiceProvider>()).Returns(handler);
		return (factory, handler);
	}

	public class Factories
	{
		private static readonly string[] All =
			[Method.Get.Value, Method.Head.Value, Method.Post.Value, Method.Put.Value, Method.Patch.Value, Method.Delete.Value];

		public static TheoryData<string> Methods =>
			[.. All];

		[Theory]
		[MemberData(nameof(Methods))]
		public void Calls_Each_Factory_Once(string method)
		{
			// Arrange
			var router = new Router();
			var (literal, _) = NewFactory();
			var (param, _) = NewFactory();
			var (catchAll, _) = NewFactory();
			var m = Method.Wrap(method);
			router.Add(m, Template.Parse("/posts", router.Types), literal);
			router.Add(m, Template.Parse("/posts/{id:int}", router.Types), param);
			router.Add(m, Template.Parse("/files/{path:*}", router.Types), catchAll);

			// Act
			router.Lock(NewProvider());

			// Assert
			_ = literal.Received(1).Invoke(Arg.Any<IServiceProvider>());
			_ = param.Received(1).Invoke(Arg.Any<IServiceProvider>());
			_ = catchAll.Received(1).Invoke(Arg.Any<IServiceProvider>());
		}

		[Fact]
		public void Calls_Factories_For_Every_Method()
		{
			// Arrange
			var router = new Router();
			var factories = All.Select(m => (Method: Method.Wrap(m), NewFactory().Factory)).ToList();
			foreach (var (method, factory) in factories)
			{
				router.Add(method, Template.Parse("/posts", router.Types), factory);
			}

			// Act
			router.Lock(NewProvider());

			// Assert
			Assert.All(factories, x => x.Factory.Received(1).Invoke(Arg.Any<IServiceProvider>()));
		}

		[Fact]
		public void Passes_Given_Provider_To_Factory()
		{
			// Arrange
			var router = new Router();
			var (factory, _) = NewFactory();
			var sp = NewProvider();
			router.Map("/posts").Get(factory);

			// Act
			router.Lock(sp);

			// Assert
			_ = factory.Received(1).Invoke(sp);
		}

		[Fact]
		public void Factory_Is_Not_Called_By_Match()
		{
			// Arrange
			var router = new Router();
			var (factory, _) = NewFactory();
			router.Map("/posts").Get(factory);
			router.Lock(NewProvider());
			factory.ClearReceivedCalls();

			// Act
			_ = router.Match(Method.Get, "/posts");
			_ = router.Match(Method.Get, "/posts");

			// Assert
			Assert.Empty(factory.ReceivedCalls());
		}
	}

	public class Resolved
	{
		[Fact]
		public void Match_Returns_Handler_From_Factory()
		{
			// Arrange
			var router = new Router();
			var (factory, handler) = NewFactory();
			router.Map("/posts/{id:int}").Get(factory);
			router.Lock(NewProvider());

			// Act
			var result = router.Match(Method.Get, $"/posts/{Rnd.Int}");

			// Assert
			Assert.Same(handler, Assert.IsType<Matched>(result).Handler);
		}

		[Fact]
		public void Head_Falls_Back_To_Handler_From_Get_Factory()
		{
			// Arrange
			var router = new Router();
			var (factory, handler) = NewFactory();
			router.Map("/posts").Get(factory);
			router.Lock(NewProvider());

			// Act
			var result = router.Match(Method.Head, "/posts");

			// Assert
			Assert.Same(handler, Assert.IsType<Matched>(result).Handler);
		}

		[Fact]
		public void Catch_All_Returns_Handler_From_Factory()
		{
			// Arrange
			var router = new Router();
			var (factory, handler) = NewFactory();
			router.Map("/files/{path:*}").Get(factory);
			router.Lock(NewProvider());

			// Act
			var result = router.Match(Method.Get, "/files/a/b.txt");

			// Assert
			Assert.Same(handler, Assert.IsType<Matched>(result).Handler);
		}

		[Fact]
		public void Async_Handler_Is_Matched_Unchanged()
		{
			// Arrange
			var router = new Router();
			var handler = Substitute.For<HandlerAsync>();
			router.Map("/posts").Get(handler);

			// Act
			router.Lock(NewProvider());

			// Assert
			Assert.Same(handler, Assert.IsType<Matched>(router.Match(Method.Get, "/posts")).Handler);
		}

		[Fact]
		public async Task Sync_Handler_Is_Matched_As_Async_Handler_Returning_Its_Result()
		{
			// Arrange
			var router = new Router();
			var reply = Reply.Text(Rnd.Str).AssertOk();
			var handler = Substitute.For<Handler>();
			var req = Extensions.ReqExtensions_Tests.Helpers.NewReq();
			_ = handler(req).Returns(reply);
			router.Map("/posts").Get(handler);
			router.Lock(NewProvider());
			var matched = Assert.IsType<Matched>(router.Match(Method.Get, "/posts"));

			// Act
			var result = await matched.Handler(req);

			// Assert
			Assert.Same(reply, result.AssertOk());
			_ = handler.Received(1).Invoke(req);
		}

		[Fact]
		public void Sync_Handler_Is_Not_Called_By_Lock()
		{
			// Arrange
			var router = new Router();
			var handler = Substitute.For<Handler>();
			router.Map("/posts").Get(handler);

			// Act
			router.Lock(NewProvider());

			// Assert
			Assert.Empty(handler.ReceivedCalls());
		}
	}

	public class Errors
	{
		[Fact]
		public void Factory_Throwing_Throws_InvalidOperationException_Naming_Route()
		{
			// Arrange
			var router = new Router();
			var inner = new InvalidOperationException(Rnd.Str);
			var factory = Substitute.For<HandlerFactory>();
			factory.When(x => x(Arg.Any<IServiceProvider>())).Do(_ => throw inner);
			router.Map("/posts/{id:int}").Post(factory);

			// Act
			var result = Record.Exception(() => router.Lock(NewProvider()));

			// Assert
			var ex = Assert.IsType<InvalidOperationException>(result);
			Assert.Contains("POST", ex.Message);
			Assert.Contains("/posts/{id:int}", ex.Message);
			Assert.Same(inner, ex.InnerException);
		}

		[Fact]
		public void Catch_All_Factory_Throwing_Names_Route()
		{
			// Arrange
			var router = new Router();
			var inner = new InvalidOperationException(Rnd.Str);
			var factory = Substitute.For<HandlerFactory>();
			factory.When(x => x(Arg.Any<IServiceProvider>())).Do(_ => throw inner);
			router.Map("/files/{path:*}").Get(factory);

			// Act
			var result = Record.Exception(() => router.Lock(NewProvider()));

			// Assert
			var ex = Assert.IsType<InvalidOperationException>(result);
			Assert.Contains("GET", ex.Message);
			Assert.Contains("/files/{path:*}", ex.Message);
			Assert.Same(inner, ex.InnerException);
		}

		[Fact]
		public void Map_After_Lock_Throws()
		{
			// Arrange
			var router = new Router();
			router.Lock(NewProvider());

			// Act
			var result = Record.Exception(() => router.Map("/posts"));

			// Assert
			var ex = Assert.IsType<InvalidOperationException>(result);
			Assert.Contains("locked", ex.Message);
		}

		[Fact]
		public void Locking_Twice_Throws()
		{
			// Arrange
			var router = new Router();
			router.Lock(NewProvider());

			// Act
			var result = Record.Exception(() => router.Lock(NewProvider()));

			// Assert
			_ = Assert.IsType<InvalidOperationException>(result);
		}

		[Fact]
		public void Locking_Twice_Does_Not_Call_Factory_Again()
		{
			// Arrange
			var router = new Router();
			var (factory, _) = NewFactory();
			router.Map("/posts").Get(factory);
			router.Lock(NewProvider());

			// Act
			_ = Record.Exception(() => router.Lock(NewProvider()));

			// Assert
			_ = factory.Received(1).Invoke(Arg.Any<IServiceProvider>());
		}
	}
}
