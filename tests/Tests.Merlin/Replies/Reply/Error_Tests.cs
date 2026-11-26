// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Microsoft.AspNetCore.Http;
using Wrap;
using Wrap.Testing;
using static Merlin.Replies.Helpers;

namespace Merlin.Replies.Reply_Tests;

public class Error_Tests
{
	public class Status
	{
		// kinds by name, as FailureKind is not serializable and Test Explorer could not list each row
		private static readonly Dictionary<string, FailureKind> kinds = new()
		{
			[nameof(FailureKind.Invalid)] = FailureKind.Invalid,
			[nameof(FailureKind.Unauthenticated)] = FailureKind.Unauthenticated,
			[nameof(FailureKind.Forbidden)] = FailureKind.Forbidden,
			[nameof(FailureKind.NotFound)] = FailureKind.NotFound,
			[nameof(FailureKind.Conflict)] = FailureKind.Conflict,
			[nameof(FailureKind.Unavailable)] = FailureKind.Unavailable,
			[nameof(FailureKind.Timeout)] = FailureKind.Timeout,
			[nameof(FailureKind.Unexpected)] = FailureKind.Unexpected,
			[nameof(FailureKind.Unspecified)] = FailureKind.Unspecified,
		};

		public static FailureKind KindOf(string name) =>
			kinds[name];

		public static TheoryData<string, int> Kinds =>
			new()
			{
				{ nameof(FailureKind.Invalid), StatusCodes.Status400BadRequest },
				{ nameof(FailureKind.Unauthenticated), StatusCodes.Status401Unauthorized },
				{ nameof(FailureKind.Forbidden), StatusCodes.Status403Forbidden },
				{ nameof(FailureKind.NotFound), StatusCodes.Status404NotFound },
				{ nameof(FailureKind.Conflict), StatusCodes.Status409Conflict },
				{ nameof(FailureKind.Unavailable), StatusCodes.Status503ServiceUnavailable },
				{ nameof(FailureKind.Timeout), StatusCodes.Status504GatewayTimeout },
				{ nameof(FailureKind.Unexpected), StatusCodes.Status500InternalServerError },
				{ nameof(FailureKind.Unspecified), StatusCodes.Status500InternalServerError },
			};

		[Theory]
		[MemberData(nameof(Kinds))]
		public void Maps_Kind_To_Status(string kind, int expected)
		{
			// Arrange
			var failure = R.Fail(Rnd.Str).Knd(KindOf(kind));

			// Act
			var result = Reply.Error(failure);

			// Assert
			Assert.Equal(expected, result.AssertOk().StatusCode);
		}

		[Fact]
		public void Defaults_To_500()
		{
			// Arrange
			var failure = R.Fail(Rnd.Str);

			// Act
			var result = Reply.Error(failure);

			// Assert
			Assert.Equal(StatusCodes.Status500InternalServerError, result.AssertOk().StatusCode);
		}

		[Fact]
		public void Custom_Kind_Is_500()
		{
			// Arrange
			var failure = R.Fail(Rnd.Str).Knd(new($"Tests.{Rnd.Str}"));

			// Act
			var result = Reply.Error(failure);

			// Assert
			Assert.Equal(StatusCodes.Status500InternalServerError, result.AssertOk().StatusCode);
		}

		[Theory]
		[InlineData(typeof(TimeoutException), StatusCodes.Status504GatewayTimeout)]
		[InlineData(typeof(InvalidOperationException), StatusCodes.Status500InternalServerError)]
		public void Uses_Kind_Chosen_From_Exception(Type exception, int expected)
		{
			// Arrange
			var failure = new Failure((Exception)Activator.CreateInstance(exception)!);

			// Act
			var result = Reply.Error(failure);

			// Assert
			Assert.Equal(expected, result.AssertOk().StatusCode);
		}

		[Fact]
		public async Task Writes_Status()
		{
			// Arrange
			var reply = Reply.Error(R.Fail(Rnd.Str).Knd(FailureKind.NotFound)).AssertOk();

			// Act
			var (_, response, _) = await WriteAsync(reply);

			// Assert
			Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
		}
	}

	public class Body
	{
		[Theory]
		[MemberData(nameof(Status.Kinds), MemberType = typeof(Status))]
		public async Task Does_Not_Write_Failure_Details(string kind, int _)
		{
			// Arrange
			var message = Rnd.Str;
			var (type, function) = (Rnd.Str, Rnd.Str);
			var failure = R.Fail(message).Ctx(type, function).Knd(Status.KindOf(kind));
			var reply = Reply.Error(failure).AssertOk();

			// Act
			var (_, _, body) = await WriteAsync(reply);

			// Assert
			Assert.DoesNotContain(message, body);
			Assert.DoesNotContain(type, body);
			Assert.DoesNotContain(function, body);
		}
	}
}
