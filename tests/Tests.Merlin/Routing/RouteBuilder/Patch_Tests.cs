// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Replies;
using Wrap;

namespace Merlin.Routing.RouteBuilder_Tests;

public class Patch_Tests
{
	// adding a second handler for the same method and template throws only if the builder added the first
	private static Exception? AddAgain(Router router, Template template, string method) =>
		Record.Exception(() => router.Add(method, template, Substitute.For<Handler>()));

	[Fact]
	public void Adds_Async_Handler_For_PATCH()
	{
		// Arrange
		var router = new Router();
		var template = Template.Parse("/posts", router.Types);
		var builder = new RouteBuilder(router, template);

		// Act
		builder.Patch(Substitute.For<Handler>());

		// Assert
		Assert.IsType<InvalidOperationException>(AddAgain(router, template, "PATCH"));
	}

	[Fact]
	public void Adds_Sync_Handler_For_PATCH()
	{
		// Arrange
		var router = new Router();
		var template = Template.Parse("/posts", router.Types);
		var builder = new RouteBuilder(router, template);

		// Act
		builder.Patch(Substitute.For<Func<Req, Result<Reply>>>());

		// Assert
		Assert.IsType<InvalidOperationException>(AddAgain(router, template, "PATCH"));
	}

	[Fact]
	public void Does_Not_Add_Handler_For_Other_Methods()
	{
		// Arrange
		var router = new Router();
		var template = Template.Parse("/posts", router.Types);
		var builder = new RouteBuilder(router, template);
		var other = "PATCH" == "GET" ? "POST" : "GET";

		// Act
		builder.Patch(Substitute.For<Handler>());

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
		var async = builder.Patch(Substitute.For<Handler>());

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
		var sync = builder.Patch(Substitute.For<Func<Req, Result<Reply>>>());

		// Assert
		Assert.Same(builder, sync);
	}
}
