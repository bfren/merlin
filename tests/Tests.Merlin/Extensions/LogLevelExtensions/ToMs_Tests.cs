// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using MsLevel = Microsoft.Extensions.Logging.LogLevel;
using WrapLevel = Wrap.Logging.LogLevel;

namespace Merlin.Extensions.LogLevelExtensions_Tests;

public class ToMs_Tests
{
	[Theory]
	[InlineData(WrapLevel.Verbose, MsLevel.Trace)]
	[InlineData(WrapLevel.Debug, MsLevel.Debug)]
	[InlineData(WrapLevel.Information, MsLevel.Information)]
	[InlineData(WrapLevel.Warning, MsLevel.Warning)]
	[InlineData(WrapLevel.Error, MsLevel.Error)]
	[InlineData(WrapLevel.Fatal, MsLevel.Critical)]
	[InlineData(WrapLevel.Unknown, MsLevel.Warning)]
	public void Maps_Level(WrapLevel input, MsLevel expected)
	{
		// Arrange

		// Act
		var result = input.ToMs();

		// Assert
		Assert.Equal(expected, result);
	}

	[Fact]
	public void Maps_Every_Defined_Level_To_A_Defined_Level()
	{
		// Arrange
		var levels = Enum.GetValues<WrapLevel>();

		// Act
		var result = levels.Select(x => x.ToMs());

		// Assert
		Assert.All(result, x => Assert.True(Enum.IsDefined(x) && x != MsLevel.None));
	}

	[Fact]
	public void Maps_Combined_Flags_To_Warning()
	{
		// Arrange
		var input = WrapLevel.Debug | WrapLevel.Error;

		// Act
		var result = input.ToMs();

		// Assert
		Assert.Equal(MsLevel.Warning, result);
	}
}
