// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Routing.ParamType_Tests;

public class Create_Tests
{
	[Fact]
	public void Sets_Name_Type_And_Priority()
	{
		// Arrange
		var name = Rnd.Str;
		var priority = Rnd.Int;

		// Act
		var result = ParamType.Create<int>(name, priority);

		// Assert
		Assert.Equal(name, result.Name);
		Assert.Equal(typeof(int), result.Type);
		Assert.Equal(priority, result.Priority);
	}

	[Fact]
	public void TryParse_Returns_Parsed_Value()
	{
		// Arrange
		var type = ParamType.Create<int>(Rnd.Str, Rnd.Int);
		var value = Rnd.Int;

		// Act
		var result = type.TryParse(value.ToString(), out var parsed);

		// Assert
		Assert.True(result);
		Assert.Equal(value, Assert.IsType<int>(parsed));
	}

	[Fact]
	public void TryParse_Returns_False_And_Null_When_Invalid()
	{
		// Arrange
		var type = ParamType.Create<int>(Rnd.Str, Rnd.Int);

		// Act
		var result = type.TryParse("abc", out var parsed);

		// Assert
		Assert.False(result);
		Assert.Null(parsed);
	}

	[Fact]
	public void TryParse_Uses_Invariant_Number_Format()
	{
		// Arrange
		var type = ParamType.Create<decimal>(Rnd.Str, Rnd.Int);

		// Act
		var result = type.TryParse("1.5", out var parsed);

		// Assert
		Assert.True(result);
		Assert.Equal(1.5m, parsed);
	}
}
