// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Wrap.Testing;
using static Merlin.Extensions.ReqExtensions_Tests.Helpers;

namespace Merlin.Extensions.ReqExtensions_Tests;

public class Query_Tests
{
	private const string Message = "Query value {Name} is missing or not a valid {Type}.";

	public class Valid
	{
		[Fact]
		public void Returns_Parsed_Int()
		{
			// Arrange
			var value = Rnd.Int;
			var req = NewReq(query: $"?page={value}");

			// Act
			var result = req.Query<int>("page");

			// Assert
			result.AssertOk(value);
		}

		[Fact]
		public void Returns_String()
		{
			// Arrange
			var value = Rnd.Str;
			var req = NewReq(query: $"?q={value}");

			// Act
			var result = req.Query<string>("q");

			// Assert
			result.AssertOk(value);
		}

		[Fact]
		public void Returns_Parsed_Guid()
		{
			// Arrange
			var value = Guid.NewGuid();
			var req = NewReq(query: $"?id={value}");

			// Act
			var result = req.Query<Guid>("id");

			// Assert
			result.AssertOk(value);
		}

		[Theory]
		[InlineData("true", true)]
		[InlineData("False", false)]
		public void Returns_Parsed_Bool(string input, bool expected)
		{
			// Arrange
			var req = NewReq(query: $"?draft={input}");

			// Act
			var result = req.Query<bool>("draft");

			// Assert
			result.AssertOk(expected);
		}

		[Fact]
		public void Uses_Invariant_Culture()
		{
			// Arrange
			var req = NewReq(query: "?price=1.5&from=2026-11-25");

			// Act
			var price = req.Query<decimal>("price");
			var from = req.Query<DateOnly>("from");

			// Assert
			price.AssertOk(1.5m);
			from.AssertOk(new DateOnly(2026, 11, 25));
		}

		[Fact]
		public void Decodes_Value()
		{
			// Arrange
			var req = NewReq(query: "?name=Ben%20Green");

			// Act
			var result = req.Query<string>("name");

			// Assert
			result.AssertOk("Ben Green");
		}

		[Fact]
		public void Ignores_Key_Case()
		{
			// Arrange
			var value = Rnd.Int;
			var req = NewReq(query: $"?Page={value}");

			// Act
			var result = req.Query<int>("page");

			// Assert
			result.AssertOk(value);
		}

		[Fact]
		public void Repeated_Key_Uses_First_Value()
		{
			// Arrange
			var req = NewReq(query: "?page=1&page=2&tag=a&tag=b");

			// Act
			var page = req.Query<int>("page");
			var tag = req.Query<string>("tag");

			// Assert
			page.AssertOk(1);
			tag.AssertOk("a");
		}

		[Fact]
		public void Empty_Value_Is_Empty_String()
		{
			// Arrange
			var req = NewReq(query: "?q=");

			// Act
			var result = req.Query<string>("q");

			// Assert
			result.AssertOk(string.Empty);
		}
	}

	public class Failures
	{
		[Fact]
		public void Missing_Key_Returns_Failure()
		{
			// Arrange
			var name = Rnd.Str;
			var req = NewReq(query: $"?other={Rnd.Int}");

			// Act
			var result = req.Query<int>(name);

			// Assert
			result.AssertFailure(Message, name, typeof(int));
		}

		[Fact]
		public void Missing_Query_String_Returns_Failure()
		{
			// Arrange
			var req = NewReq();

			// Act
			var result = req.Query<string>("q");

			// Assert
			result.AssertFailure(Message, "q", typeof(string));
		}

		[Theory]
		[InlineData("?page=abc")]
		[InlineData("?page=")]
		[InlineData("?page=1.5")]
		public void Unparseable_Value_Returns_Failure(string query)
		{
			// Arrange
			var req = NewReq(query: query);

			// Act
			var result = req.Query<int>("page");

			// Assert
			result.AssertFailure(Message, "page", typeof(int));
		}

		[Fact]
		public void Failure_Has_Context()
		{
			// Arrange
			var req = NewReq();

			// Act
			var result = req.Query<int>(Rnd.Str);

			// Assert
			Assert.Equal(ContextOf("QueryValueMissingOrInvalidType"), result.AssertFailure().Context);
		}

		[Theory]
		[InlineData("")]
		[InlineData("?page=abc")]
		public void Failure_Has_Kind_Invalid(string query)
		{
			// Arrange
			var req = NewReq(query: query);

			// Act
			var result = req.Query<int>("page");

			// Assert
			Assert.Equal(Wrap.FailureKind.Invalid, result.AssertFailure().Kind);
		}
	}
}
