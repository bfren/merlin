// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Routing.Router_Tests;

public class Map_Tests
{
	[Fact]
	public void Uses_Custom_Param_Types()
	{
		// Arrange
		var router = new Router();
		router.Types.Add<decimal>("money");

		// Act
		var result = Record.Exception(() => router.Map("/{price:money}"));

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public void Invalid_Template_Throws()
	{
		// Arrange
		var router = new Router();

		// Act
		var result = Record.Exception(() => router.Map("/{id"));

		// Assert
		Assert.IsType<InvalidOperationException>(result);
	}

	[Fact]
	public void Throws_When_Locked()
	{
		// Arrange
		var router = new Router();
		router.Lock();

		// Act
		var result = Record.Exception(() => router.Map("/posts"));

		// Assert
		var ex = Assert.IsType<InvalidOperationException>(result);
		Assert.Contains("locked", ex.Message);
	}

	public class Names
	{
		[Fact]
		public void Duplicate_Name_Throws()
		{
			// Arrange
			var router = new Router();
			var name = Rnd.Str;
			router.Map("/a", name);

			// Act
			var result = Record.Exception(() => router.Map("/b", name));

			// Assert
			var ex = Assert.IsType<InvalidOperationException>(result);
			Assert.Contains(name, ex.Message);
		}

		[Fact]
		public void Names_Are_Case_Sensitive()
		{
			// Arrange
			var router = new Router();
			router.Map("/a", "posts");

			// Act
			var result = Record.Exception(() => router.Map("/b", "POSTS"));

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void Unnamed_Routes_Do_Not_Conflict()
		{
			// Arrange
			var router = new Router();
			router.Map("/a");

			// Act
			var result = Record.Exception(() => router.Map("/b"));

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void Name_Is_Not_Used_When_Template_Is_Invalid()
		{
			// Arrange
			var router = new Router();
			var name = Rnd.Str;
			Assert.Throws<InvalidOperationException>(() => router.Map("/{id", name));

			// Act
			var result = Record.Exception(() => router.Map("/a", name));

			// Assert
			Assert.Null(result);
		}
	}
}
