// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Merlin.Routing.RouteBuilder_Tests;

public class Map_Tests
{
	[Fact]
	public void Maps_On_Same_Router()
	{
		// Arrange
		var router = new Router();
		var first = router.Map("/a");
		var name = Rnd.Str;

		// Act
		var second = first.Map("/b", name);

		// Assert
		Assert.NotSame(first, second);
		Assert.Throws<InvalidOperationException>(() => router.Map("/c", name));
	}

	[Fact]
	public void Chains_Routes()
	{
		// Arrange
		var router = new Router();
		var a = Template.Parse("/a", router.Types);
		var b = Template.Parse("/b", router.Types);

		// Act
		router.Map("/a").Get(Substitute.For<HandlerAsync>())
			.Map("/b").Post(Substitute.For<HandlerAsync>());

		// Assert
		Assert.Throws<InvalidOperationException>(() => router.Add(Method.Get, a, Substitute.For<HandlerAsync>()));
		Assert.Throws<InvalidOperationException>(() => router.Add(Method.Post, b, Substitute.For<HandlerAsync>()));
	}

	[Fact]
	public void Throws_When_Router_Is_Locked()
	{
		// Arrange
		var router = new Router();
		var builder = router.Map("/a");
		router.Lock(Substitute.For<IServiceProvider>(), new ServiceCollection());

		// Act
		var result = Record.Exception(() => builder.Map("/b"));

		// Assert
		Assert.IsType<InvalidOperationException>(result);
	}
}
