// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Globalization;
using System.Text.Json;
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
		private static readonly Dictionary<string, FailureKind> FailureKinds = new()
		{
			[nameof(FailureKind.Invalid)] = FailureKind.Invalid,
			[nameof(FailureKind.Unauthenticated)] = FailureKind.Unauthenticated,
			[nameof(FailureKind.Forbidden)] = FailureKind.Forbidden,
			[nameof(FailureKind.NotFound)] = FailureKind.NotFound,
			[nameof(FailureKind.Conflict)] = FailureKind.Conflict,
			[nameof(FailureKind.Cancelled)] = FailureKind.Cancelled,
			[nameof(FailureKind.Unavailable)] = FailureKind.Unavailable,
			[nameof(FailureKind.Timeout)] = FailureKind.Timeout,
			[nameof(FailureKind.Unexpected)] = FailureKind.Unexpected,
			[nameof(FailureKind.Unspecified)] = FailureKind.Unspecified,
		};

		public static FailureKind KindOf(string name) =>
			FailureKinds[name];

		public static TheoryData<string, int> Kinds =>
			new()
			{
				{ nameof(FailureKind.Invalid), StatusCodes.Status400BadRequest },
				{ nameof(FailureKind.Unauthenticated), StatusCodes.Status401Unauthorized },
				{ nameof(FailureKind.Forbidden), StatusCodes.Status403Forbidden },
				{ nameof(FailureKind.NotFound), StatusCodes.Status404NotFound },
				{ nameof(FailureKind.Conflict), StatusCodes.Status409Conflict },
				{ nameof(FailureKind.Cancelled), StatusCodes.Status499ClientClosedRequest },
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
			var failure = R.Fail(Rnd.Str).WithKind(KindOf(kind));

			// Act
			var result = Reply.Error(failure);

			// Assert
			Assert.Equal(expected, result.AssertOk().Status.Value);
		}

		[Fact]
		public void Defaults_To_500()
		{
			// Arrange
			var failure = R.Fail(Rnd.Str);

			// Act
			var result = Reply.Error(failure);

			// Assert
			Assert.Equal(Domain.Status.InternalServerError, result.AssertOk().Status);
		}

		[Fact]
		public void Custom_Kind_Is_500()
		{
			// Arrange
			var failure = R.Fail(Rnd.Str).WithKind(new($"Tests.{Rnd.Str}"));

			// Act
			var result = Reply.Error(failure);

			// Assert
			Assert.Equal(Domain.Status.InternalServerError, result.AssertOk().Status);
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
			Assert.Equal(expected, result.AssertOk().Status.Value);
		}

		[Fact]
		public async Task Writes_Status()
		{
			// Arrange
			var reply = Reply.Error(R.Fail(Rnd.Str).WithKind(FailureKind.NotFound)).AssertOk();

			// Act
			var (_, response, _) = await WriteAsync(reply);

			// Assert
			Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
		}
	}

	public class Body
	{
		public static TheoryData<string> ClientKinds =>
			[
				nameof(FailureKind.Invalid),
				nameof(FailureKind.Unauthenticated),
				nameof(FailureKind.Forbidden),
				nameof(FailureKind.NotFound),
				nameof(FailureKind.Conflict),
			];

		public static TheoryData<string> ServerKinds =>
			[
				nameof(FailureKind.Unavailable),
				nameof(FailureKind.Timeout),
				nameof(FailureKind.Unexpected),
				nameof(FailureKind.Unspecified),
			];

		private static async Task<JsonElement> ProblemAsync(Failure failure)
		{
			var (_, _, body) = await WriteAsync(Reply.Error(failure).AssertOk());
			return JsonElement.Parse(body);
		}

		[Theory]
		[MemberData(nameof(ClientKinds))]
		public async Task Client_Error_Writes_Message_As_Detail(string kind)
		{
			// Arrange
			var message = Rnd.Str;
			var failure = R.Fail(message).WithKind(Status.KindOf(kind));

			// Act
			var result = await ProblemAsync(failure);

			// Assert
			Assert.Equal(message, result.GetProperty("detail").GetString());
		}

		[Theory]
		[MemberData(nameof(ClientKinds))]
		public async Task Client_Error_Detail_Fills_Named_Placeholders_From_Args_In_Order(string kind)
		{
			// Arrange
			var (name, number) = (Rnd.Str, Rnd.Int);
			var failure = R.Fail("Value {Name} is not {Number}.", name, number).WithKind(Status.KindOf(kind));

			// Act
			var result = await ProblemAsync(failure);

			// Assert
			Assert.Equal($"Value {name} is not {number}.", result.GetProperty("detail").GetString());
		}

		[Fact]
		public async Task Client_Error_Detail_Formats_Args_With_Default_Culture()
		{
			// Arrange
			var value = 1234.5; // a decimal point, which de-DE writes as a comma
			var failure = R.Fail("Value {Number} is too big.", value).WithKind(FailureKind.Invalid);
			var server = CultureInfo.CurrentCulture;
			CultureInfo.CurrentCulture = new("de-DE");
			Assert.NotEqual(value.ToString(F.DefaultCulture), value.ToString(CultureInfo.CurrentCulture));

			// Act
			JsonElement result;
			try
			{
				result = await ProblemAsync(failure);
			}
			finally
			{
				CultureInfo.CurrentCulture = server;
			}

			// Assert
			Assert.Equal($"Value {value.ToString(F.DefaultCulture)} is too big.", result.GetProperty("detail").GetString());
		}

		[Theory]
		[MemberData(nameof(ServerKinds))]
		public async Task Server_Error_Leaves_Out_Detail(string kind)
		{
			// Arrange
			var message = Rnd.Str;
			var failure = R.Fail(message).WithKind(Status.KindOf(kind));

			// Act
			var result = await ProblemAsync(failure);

			// Assert
			Assert.False(result.TryGetProperty("detail", out _));
			Assert.DoesNotContain(message, result.GetRawText());
		}

		[Fact]
		public async Task Cancelled_Leaves_Out_Detail()
		{
			// Arrange
			var message = Rnd.Str;
			var failure = R.Fail(message).WithKind(FailureKind.Cancelled);

			// Act
			var result = await ProblemAsync(failure);

			// Assert
			// 499 is a 4xx, but the client has gone and the message may be an exception's
			Assert.False(result.TryGetProperty("detail", out _));
			Assert.DoesNotContain(message, result.GetRawText());
		}

		[Fact]
		public async Task Custom_Kind_Leaves_Out_Detail()
		{
			// Arrange
			var message = Rnd.Str;
			var failure = R.Fail(message).WithKind(new($"Tests.{Rnd.Str}"));

			// Act
			var result = await ProblemAsync(failure);

			// Assert
			Assert.False(result.TryGetProperty("detail", out _));
			Assert.DoesNotContain(message, result.GetRawText());
		}

		[Fact]
		public async Task Exception_Failure_Leaves_Out_Exception_Message()
		{
			// Arrange
			var message = Rnd.Str;
			var failure = new Failure(new InvalidOperationException(message));

			// Act
			var result = await ProblemAsync(failure);

			// Assert
			Assert.DoesNotContain(message, result.GetRawText());
		}

		[Theory]
		[MemberData(nameof(Status.Kinds), MemberType = typeof(Status))]
		public async Task Does_Not_Write_Context(string kind, int _)
		{
			// Arrange
			var (type, function) = (Rnd.Str, Rnd.Str);
			var failure = R.Fail(Rnd.Str).WithContext(type, function).WithKind(Status.KindOf(kind));

			// Act
			var result = await ProblemAsync(failure);

			// Assert
			Assert.DoesNotContain(type, result.GetRawText());
			Assert.DoesNotContain(function, result.GetRawText());
		}
	}
}
