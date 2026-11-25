// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Wrap.Testing;
using static Merlin.Extensions.ReqExtensions_Tests.Helpers;

namespace Merlin.Extensions.ReqExtensions_Tests;

public class QueryOrNone_Tests
{
	[Fact]
	public void Returns_Some_Parsed_Value()
	{
		// Arrange
		var value = Rnd.Int;
		var req = NewReq(query: $"?page={value}");

		// Act
		var result = req.QueryOrNone<int>("page");

		// Assert
		Assert.Equal(value, result.AssertSome());
	}

	[Fact]
	public void Returns_Some_String()
	{
		// Arrange
		var value = Rnd.Str;
		var req = NewReq(query: $"?q={value}");

		// Act
		var result = req.QueryOrNone<string>("q");

		// Assert
		Assert.Equal(value, result.AssertSome());
	}

	[Fact]
	public void Ignores_Key_Case()
	{
		// Arrange
		var value = Rnd.Int;
		var req = NewReq(query: $"?PAGE={value}");

		// Act
		var result = req.QueryOrNone<int>("page");

		// Assert
		Assert.Equal(value, result.AssertSome());
	}

	[Fact]
	public void Returns_None_When_Missing()
	{
		// Arrange
		var req = NewReq(query: $"?other={Rnd.Int}");

		// Act
		var result = req.QueryOrNone<int>("page");

		// Assert
		result.AssertNone();
	}

	[Theory]
	[InlineData("?page=abc")]
	[InlineData("?page=")]
	[InlineData("?page=1&page=2")]
	public void Returns_None_When_Value_Cannot_Be_Parsed(string query)
	{
		// Arrange
		var req = NewReq(query: query);

		// Act
		var result = req.QueryOrNone<int>("page");

		// Assert
		result.AssertNone();
	}
}
