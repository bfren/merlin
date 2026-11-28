// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Domain;
using Merlin.Replies;
using Microsoft.Extensions.DependencyInjection;
using Wrap;

namespace Merlin.Routing.RouteBuilder_Tests;

public class Get_Tests
{
	// adding a second handler for the same method and template throws only if the builder added the first
	private static Exception? AddAgain(Router router, Template template, Method method) =>
		Record.Exception(() => router.Add(method, template, Substitute.For<HandlerAsync>()));

	[Fact]
	public void Adds_Async_Handler_For_GET()
	{
		// Arrange
		var router = new Router();
		var template = Template.Parse("/posts", router.Types);
		var builder = new RouteBuilder(router, template);

		// Act
		builder.Get(Substitute.For<HandlerAsync>());

		// Assert
		Assert.IsType<InvalidOperationException>(AddAgain(router, template, Method.Get));
	}

	[Fact]
	public void Adds_Sync_Handler_For_GET()
	{
		// Arrange
		var router = new Router();
		var template = Template.Parse("/posts", router.Types);
		var builder = new RouteBuilder(router, template);

		// Act
		builder.Get(Substitute.For<Handler>());

		// Assert
		Assert.IsType<InvalidOperationException>(AddAgain(router, template, Method.Get));
	}

	[Fact]
	public void Does_Not_Add_Handler_For_Other_Methods()
	{
		// Arrange
		var router = new Router();
		var template = Template.Parse("/posts", router.Types);
		var builder = new RouteBuilder(router, template);
		var other = Method.Post;

		// Act
		builder.Get(Substitute.For<HandlerAsync>());

		// Assert
		Assert.Null(AddAgain(router, template, other));
	}

	[Fact]
	public void Returns_Same_Builder()
	{
		// Arrange
		var router = new Router();
		var builder = router.Map("/posts");

		// Act
		var async = builder.Get(Substitute.For<HandlerAsync>());

		// Assert
		Assert.Same(builder, async);
	}

	[Fact]
	public void Sync_Overload_Returns_Same_Builder()
	{
		// Arrange
		var router = new Router();
		var builder = router.Map("/posts");

		// Act
		var sync = builder.Get(Substitute.For<Handler>());

		// Assert
		Assert.Same(builder, sync);
	}

	[Fact]
	public void Adds_Factory_For_GET()
	{
		// Arrange
		var router = new Router();
		var template = Template.Parse("/posts", router.Types);
		var builder = new RouteBuilder(router, template);

		// Act
		builder.Get(Substitute.For<HandlerFactory>());

		// Assert
		Assert.IsType<InvalidOperationException>(AddAgain(router, template, Method.Get));
	}

	[Fact]
	public void Factory_Overload_Returns_Same_Builder()
	{
		// Arrange
		var router = new Router();
		var builder = router.Map("/posts");

		// Act
		var factory = builder.Get(Substitute.For<HandlerFactory>());

		// Assert
		Assert.Same(builder, factory);
	}

	[Fact]
	public void Does_Not_Call_Factory()
	{
		// Arrange
		var router = new Router();
		var factory = Substitute.For<HandlerFactory>();

		// Act
		router.Map("/posts").Get(factory);

		// Assert
		Assert.Empty(factory.ReceivedCalls());
	}

	[Fact]
	public void Lambda_Taking_Provider_Is_Added_As_Factory()
	{
		// Arrange
		var router = new Router();
		var sp = Substitute.For<IServiceProvider>();
		IServiceProvider? received = null;

		// Act
		router.Map("/posts").Get(sp => { received = sp; return Substitute.For<HandlerAsync>(); });
		router.Lock(sp, new ServiceCollection());

		// Assert
		Assert.NotNull(received);
	}
}
