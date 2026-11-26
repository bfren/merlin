// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Wrap.Testing;

namespace Merlin.Extensions.ReqExtensions_Tests;

public partial class Form_Tests
{
	public class Arrays
	{
		[Fact]
		public async Task Binds_Repeated_Field_As_Array()
		{
			// Arrange

			// Act
			var result = await Bind("tags=a&tags=b&tags=c");

			// Assert
			Assert.Equal(["a", "b", "c"], result.AssertOk().Tags ?? []);
		}

		[Fact]
		public async Task Binds_Single_Value_As_Array()
		{
			// Arrange

			// Act
			var result = await Bind("tags=a");

			// Assert
			Assert.Equal(["a"], result.AssertOk().Tags ?? []);
		}

		[Fact]
		public async Task Absent_Is_Default()
		{
			// Arrange

			// Act
			var result = await Bind("title=x");

			// Assert
			// as in JSON: PostDto sets no default for Tags, so it stays null
			Assert.Null(result.AssertOk().Tags);
		}
	}
}
