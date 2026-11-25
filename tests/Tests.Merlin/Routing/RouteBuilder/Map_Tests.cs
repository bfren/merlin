// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

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
		router.Map("/a").Get(Substitute.For<Handler>())
			.Map("/b").Post(Substitute.For<Handler>());

		// Assert
		Assert.Throws<InvalidOperationException>(() => router.Add("GET", a, Substitute.For<Handler>()));
		Assert.Throws<InvalidOperationException>(() => router.Add("POST", b, Substitute.For<Handler>()));
	}

	[Fact]
	public void Throws_When_Router_Is_Locked()
	{
		// Arrange
		var router = new Router();
		var builder = router.Map("/a");
		router.Lock();

		// Act
		var result = Record.Exception(() => builder.Map("/b"));

		// Assert
		Assert.IsType<InvalidOperationException>(result);
	}
}
