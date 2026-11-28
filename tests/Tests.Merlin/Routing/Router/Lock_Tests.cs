// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Domain;
using Merlin.Replies;
using Microsoft.Extensions.DependencyInjection;
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

	public sealed class Thing;

	// a router with one route whose factory resolves Thing, recording what it got
	private static (Router Router, Func<object?> Resolved) MapResolvingThing()
	{
		var router = new Router();
		object? resolved = null;
		router.Map("/things").Get(sp => { resolved = sp.GetService(typeof(Thing)); return Substitute.For<HandlerAsync>(); });
		return (router, () => resolved);
	}

	private static IServiceProvider ProviderWith(Thing thing)
	{
		var sp = NewProvider();
		_ = sp.GetService(typeof(Thing)).Returns(thing);
		return sp;
	}

	public class Singletons
	{
		[Fact]
		public void Singleton_Is_Resolved()
		{
			// Arrange
			var (router, resolved) = MapResolvingThing();
			var thing = new Thing();
			var services = new ServiceCollection().AddSingleton(thing);

			// Act
			router.Lock(ProviderWith(thing), services);

			// Assert
			Assert.Same(thing, resolved());
		}

		[Theory]
		[InlineData(ServiceLifetime.Transient)]
		[InlineData(ServiceLifetime.Scoped)]
		public void Other_Lifetime_Throws_Naming_Type_And_Lifetime(ServiceLifetime lifetime)
		{
			// Arrange
			var (router, _) = MapResolvingThing();
			IServiceCollection services = new ServiceCollection();
			services.Add(new ServiceDescriptor(typeof(Thing), typeof(Thing), lifetime));

			// Act
			var result = Record.Exception(() => router.Lock(ProviderWith(new()), services));

			// Assert
			var ex = Assert.IsType<InvalidOperationException>(result);
			Assert.Contains("GET", ex.Message);
			Assert.Contains("/things", ex.Message);
			var inner = Assert.IsType<InvalidOperationException>(ex.InnerException);
			Assert.Contains(nameof(Thing), inner.Message);
			Assert.Contains(lifetime.ToString(), inner.Message);
		}

		[Fact]
		public void Last_Registration_Decides_Lifetime()
		{
			// Arrange
			var (singletonLast, _) = MapResolvingThing();
			var (transientLast, _) = MapResolvingThing();
			var thing = new Thing();

			// Act
			var a = Record.Exception(() => singletonLast.Lock(ProviderWith(thing), new ServiceCollection().AddTransient<Thing>().AddSingleton(thing)));
			var b = Record.Exception(() => transientLast.Lock(ProviderWith(thing), new ServiceCollection().AddSingleton(thing).AddTransient<Thing>()));

			// Assert
			Assert.Null(a);
			_ = Assert.IsType<InvalidOperationException>(b);
		}

		[Fact]
		public void Unregistered_Type_Is_Passed_To_Given_Provider()
		{
			// Arrange
			var (router, resolved) = MapResolvingThing();
			var thing = new Thing();

			// Act
			router.Lock(ProviderWith(thing), new ServiceCollection());

			// Assert
			Assert.Same(thing, resolved());
		}

		[Fact]
		public void Plain_Handlers_Do_Not_Use_Provider()
		{
			// Arrange
			var router = new Router();
			var sp = NewProvider();
			router.Map("/a").Get(Substitute.For<Handler>());
			router.Map("/b").Get(Substitute.For<HandlerAsync>());

			// Act
			router.Lock(sp, new ServiceCollection());

			// Assert
			Assert.Empty(sp.ReceivedCalls());
		}
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
			router.Lock(NewProvider(), new ServiceCollection());

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
			router.Lock(NewProvider(), new ServiceCollection());

			// Assert
			Assert.All(factories, x => x.Factory.Received(1).Invoke(Arg.Any<IServiceProvider>()));
		}

		[Fact]
		public void Factory_Resolves_Services_From_Given_Provider()
		{
			// Arrange
			var router = new Router();
			var sp = NewProvider();
			var thing = new Thing();
			_ = sp.GetService(typeof(Thing)).Returns(thing);
			object? resolved = null;
			router.Map("/posts").Get(f => { resolved = f.GetService(typeof(Thing)); return Substitute.For<HandlerAsync>(); });

			// Act
			router.Lock(sp, new ServiceCollection());

			// Assert
			Assert.Same(thing, resolved);
		}

		[Fact]
		public void Factory_Is_Not_Called_By_Match()
		{
			// Arrange
			var router = new Router();
			var (factory, _) = NewFactory();
			router.Map("/posts").Get(factory);
			router.Lock(NewProvider(), new ServiceCollection());
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
			router.Lock(NewProvider(), new ServiceCollection());

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
			router.Lock(NewProvider(), new ServiceCollection());

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
			router.Lock(NewProvider(), new ServiceCollection());

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
			router.Lock(NewProvider(), new ServiceCollection());

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
			router.Lock(NewProvider(), new ServiceCollection());
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
			router.Lock(NewProvider(), new ServiceCollection());

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
			var result = Record.Exception(() => router.Lock(NewProvider(), new ServiceCollection()));

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
			var result = Record.Exception(() => router.Lock(NewProvider(), new ServiceCollection()));

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
			router.Lock(NewProvider(), new ServiceCollection());

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
			router.Lock(NewProvider(), new ServiceCollection());

			// Act
			var result = Record.Exception(() => router.Lock(NewProvider(), new ServiceCollection()));

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
			router.Lock(NewProvider(), new ServiceCollection());

			// Act
			_ = Record.Exception(() => router.Lock(NewProvider(), new ServiceCollection()));

			// Assert
			_ = factory.Received(1).Invoke(Arg.Any<IServiceProvider>());
		}
	}
}
