// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Replies;
using Wrap;

namespace Merlin.Routing.RouteBuilder_Tests;

public class Get_Tests
{
	// adding a second handler for the same method and template throws only if the builder added the first
	private static Exception? AddAgain(Router router, Template template, string method) =>
		Record.Exception(() => router.Add(method, template, Substitute.For<Handler>()));

	[Fact]
	public void Adds_Async_Handler_For_GET()
	{
		// Arrange
		var router = new Router();
		var template = Template.Parse("/posts", router.Types);
		var builder = new RouteBuilder(router, template);

		// Act
		builder.Get(Substitute.For<Handler>());

		// Assert
		Assert.IsType<InvalidOperationException>(AddAgain(router, template, "GET"));
	}

	[Fact]
	public void Adds_Sync_Handler_For_GET()
	{
		// Arrange
		var router = new Router();
		var template = Template.Parse("/posts", router.Types);
		var builder = new RouteBuilder(router, template);

		// Act
		builder.Get(Substitute.For<Func<Req, Result<Reply>>>());

		// Assert
		Assert.IsType<InvalidOperationException>(AddAgain(router, template, "GET"));
	}

	[Fact]
	public void Does_Not_Add_Handler_For_Other_Methods()
	{
		// Arrange
		var router = new Router();
		var template = Template.Parse("/posts", router.Types);
		var builder = new RouteBuilder(router, template);
		var other = "GET" == "GET" ? "POST" : "GET";

		// Act
		builder.Get(Substitute.For<Handler>());

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
		var async = builder.Get(Substitute.For<Handler>());

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
		var sync = builder.Get(Substitute.For<Func<Req, Result<Reply>>>());

		// Assert
		Assert.Same(builder, sync);
	}
}
