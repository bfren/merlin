// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Wrap.Testing;

namespace Merlin.Extensions.ReqExtensions_Tests;

public partial class Form_Tests
{
	public class Numbers
	{
		[Fact]
		public async Task Binds_Number_From_String()
		{
			// Arrange
			var views = Rnd.Int;

			// Act
			var result = await Bind($"views={views}");

			// Assert
			Assert.Equal(views, result.AssertOk().Views);
		}

		[Theory]
		[InlineData("abc")]
		[InlineData("1.5")]
		[InlineData("1,\"draft\":true")]
		public async Task Invalid_Number_Is_Invalid_Field(string views)
		{
			// Arrange

			// Act
			var result = await Bind($"views={Uri.EscapeDataString(views)}");

			// Assert
			AssertFailure(result, Wrap.FailureKind.Invalid, InvalidField, "views");
		}
	}
}
