// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Wrap.Testing;

namespace Merlin.Extensions.ReqExtensions_Tests;

public partial class Form_Tests
{
	public class Checkboxes
	{
		[Theory]
		[InlineData("on")]
		[InlineData("true")]
		[InlineData("1")]
		[InlineData("ON")]
		[InlineData("True")]
		public async Task Checked_Is_True(string value)
		{
			// Arrange

			// Act
			var result = await Bind($"draft={value}");

			// Assert
			Assert.True(result.AssertOk().Draft);
		}

		[Fact]
		public async Task Absent_Is_False()
		{
			// Arrange

			// Act
			var result = await Bind("title=x");

			// Assert
			Assert.False(result.AssertOk().Draft);
		}

		[Theory]
		[InlineData("off")]
		[InlineData("false")]
		[InlineData("0")]
		[InlineData("OFF")]
		[InlineData("False")]
		public async Task Unchecked_Is_False(string value)
		{
			// Arrange

			// Act
			var result = await Bind($"draft={value}");

			// Assert
			Assert.False(result.AssertOk().Draft);
		}

		[Theory]
		[InlineData("yes")]
		[InlineData("2")]
		[InlineData("")]
		public async Task Other_Value_Is_Invalid_Field(string value)
		{
			// Arrange

			// Act
			var result = await Bind($"draft={value}");

			// Assert
			// anything else fails rather than quietly becoming false
			AssertFailure(result, Wrap.FailureKind.Invalid, InvalidField, "draft");
		}
	}
}
