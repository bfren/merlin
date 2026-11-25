// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Routing.ParamTypes_Tests;

public class Get_Tests
{
	[Theory]
	[InlineData("int", typeof(int), 10)]
	[InlineData("long", typeof(long), 20)]
	[InlineData("guid", typeof(Guid), 10)]
	[InlineData("bool", typeof(bool), 10)]
	[InlineData("dateonly", typeof(DateOnly), 10)]
	[InlineData("string", typeof(string), 100)]
	public void Returns_Built_In_Type(string name, Type type, int priority)
	{
		// Arrange
		var types = new ParamTypes();

		// Act
		var result = types.Get(name);

		// Assert
		Assert.Equal(name, result.Name);
		Assert.Equal(type, result.Type);
		Assert.Equal(priority, result.Priority);
	}

	[Fact]
	public void Ignores_Case()
	{
		// Arrange
		var types = new ParamTypes();

		// Act
		var result = types.Get("GuId");

		// Assert
		Assert.Equal(typeof(Guid), result.Type);
	}

	[Fact]
	public void Unknown_Name_Throws()
	{
		// Arrange
		var types = new ParamTypes();
		var name = Rnd.Str;

		// Act
		var result = Record.Exception(() => types.Get(name));

		// Assert
		var ex = Assert.IsType<InvalidOperationException>(result);
		Assert.Contains(name, ex.Message);
	}
}
