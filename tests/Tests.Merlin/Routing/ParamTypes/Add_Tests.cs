// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Routing.ParamTypes_Tests;

public class Add_Tests
{
	[Fact]
	public void Adds_Type_With_Default_Priority()
	{
		// Arrange
		var types = new ParamTypes();
		var name = Rnd.Str;

		// Act
		types.Add<decimal>(name);

		// Assert
		var result = types.Get(name);
		Assert.Equal(name, result.Name);
		Assert.Equal(typeof(decimal), result.Type);
		Assert.Equal(50, result.Priority);
	}

	[Fact]
	public void Adds_Type_With_Priority()
	{
		// Arrange
		var types = new ParamTypes();
		var name = Rnd.Str;
		var priority = Rnd.Int;

		// Act
		types.Add<decimal>(name, priority);

		// Assert
		Assert.Equal(priority, types.Get(name).Priority);
	}

	[Fact]
	public void Does_Not_Share_Types_Between_Instances()
	{
		// Arrange
		var first = new ParamTypes();
		var second = new ParamTypes();
		var name = Rnd.Str;

		// Act
		first.Add<decimal>(name);

		// Assert
		Assert.Throws<InvalidOperationException>(() => second.Get(name));
	}

	[Fact]
	public void Reserved_Name_Throws()
	{
		// Arrange
		var types = new ParamTypes();

		// Act
		var result = Record.Exception(() => types.Add<decimal>("*"));

		// Assert
		Assert.IsType<InvalidOperationException>(result);
	}

	[Fact]
	public void Duplicate_Name_Throws()
	{
		// Arrange
		var types = new ParamTypes();
		var name = Rnd.Str;
		types.Add<decimal>(name);

		// Act
		var result = Record.Exception(() => types.Add<int>(name));

		// Assert
		Assert.IsType<ArgumentException>(result);
	}

	[Theory]
	[InlineData("int")]
	[InlineData("String")]
	public void Built_In_Name_Throws(string name)
	{
		// Arrange
		var types = new ParamTypes();

		// Act
		var result = Record.Exception(() => types.Add<decimal>(name));

		// Assert
		Assert.IsType<ArgumentException>(result);
	}
}
