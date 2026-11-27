// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Domain;
using Wrap.Testing;
using static Merlin.Extensions.ReqExtensions_Tests.Helpers;

namespace Merlin.Extensions.ReqExtensions_Tests;

public class Route_Tests
{
	private const string Message = "Route value {Name} is not a {Type}.";

	public class Matching_Type
	{
		[Fact]
		public void Returns_Int()
		{
			// Arrange
			var value = Rnd.Int;
			var req = NewReq(RouteValuesDictionary.Empty.Add("id", value));

			// Act
			var result = req.Route<int>("id");

			// Assert
			result.AssertOk(value);
		}

		[Fact]
		public void Returns_String()
		{
			// Arrange
			var value = Rnd.Str;
			var req = NewReq(RouteValuesDictionary.Empty.Add("slug", value));

			// Act
			var result = req.Route<string>("slug");

			// Assert
			result.AssertOk(value);
		}

		[Fact]
		public void Returns_Value_As_Base_Type()
		{
			// Arrange
			var value = Guid.NewGuid();
			var req = NewReq(RouteValuesDictionary.Empty.Add("id", value));

			// Act
			var result = req.Route<object>("id");

			// Assert
			Assert.Equal(value, result.AssertOk());
		}

		[Fact]
		public void Returns_Named_Value_When_There_Are_Several()
		{
			// Arrange
			var year = Rnd.Int;
			var slug = Rnd.Str;
			var req = NewReq(RouteValuesDictionary.Empty.Add("year", year).Add("slug", slug));

			// Act
			var result = req.Route<string>("slug");

			// Assert
			result.AssertOk(slug);
		}
	}

	public class Failures
	{
		[Fact]
		public void Missing_Value_Returns_Failure()
		{
			// Arrange
			var name = Rnd.Str;
			var req = NewReq();

			// Act
			var result = req.Route<int>(name);

			// Assert
			result.AssertFailure(Message, name, nameof(Int32));
		}

		[Fact]
		public void Wrong_Type_Returns_Failure()
		{
			// Arrange
			var req = NewReq(RouteValuesDictionary.Empty.Add("id", Rnd.Int));

			// Act
			var result = req.Route<long>("id");

			// Assert
			result.AssertFailure(Message, "id", nameof(Int64));
		}

		[Fact]
		public void Does_Not_Convert_To_String()
		{
			// Arrange
			var req = NewReq(RouteValuesDictionary.Empty.Add("id", Rnd.Int));

			// Act
			var result = req.Route<string>("id");

			// Assert
			result.AssertFailure(Message, "id", nameof(String));
		}

		[Fact]
		public void Failure_Has_Context()
		{
			// Arrange
			var req = NewReq();

			// Act
			var result = req.Route<int>(Rnd.Str);

			// Assert
			Assert.Equal(ContextOf("Route"), result.AssertFailure().Context);
		}

		[Fact]
		public void Missing_Value_Has_Kind_Unexpected()
		{
			// Arrange
			var req = NewReq();

			// Act
			var result = req.Route<int>(Rnd.Str);

			// Assert
			// the handler and its route template disagree: a programming error, not the client's fault
			Assert.Equal(Wrap.FailureKind.Unexpected, result.AssertFailure().Kind);
		}

		[Fact]
		public void Wrong_Type_Has_Kind_Unexpected()
		{
			// Arrange
			var req = NewReq(RouteValuesDictionary.Empty.Add("id", Rnd.Int));

			// Act
			var result = req.Route<long>("id");

			// Assert
			Assert.Equal(Wrap.FailureKind.Unexpected, result.AssertFailure().Kind);
		}
	}
}
