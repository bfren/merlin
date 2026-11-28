// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Merlin.Routing.Router_Tests;

public partial class Match_Tests
{
	public class Types
	{
		// map one route with a typed param, lock, and match a path
		private static RouteMatch MatchOne(string template, string path)
		{
			var router = new Router();
			router.Map(template).Get(NewHandler());
			router.Lock(Sp, new ServiceCollection());
			return router.Match(Method.Get, path);
		}

		private static T Value<T>(RouteMatch result, string name) =>
			Assert.IsType<T>(Assert.IsType<Matched>(result).Values[name]);

		public class DateTime
		{
			[Fact]
			public void Keeps_Offset_As_Sent()
			{
				// Arrange
				var path = "/events/2026-11-28T10:00:00+02:00";

				// Act
				var result = MatchOne("/events/{at:datetime}", path);

				// Assert
				Assert.Equal(new DateTimeOffset(2026, 11, 28, 10, 0, 0, TimeSpan.FromHours(2)), Value<DateTimeOffset>(result, "at"));
			}

			[Theory]
			[InlineData("2026-11-28T10:00:00Z")]
			[InlineData("2026-11-28T10:00:00")]
			public void Utc_Or_No_Offset_Is_Utc(string value)
			{
				// Arrange
				var path = $"/events/{value}";

				// Act
				var result = MatchOne("/events/{at:datetime}", path);

				// Assert
				var at = Value<DateTimeOffset>(result, "at");
				Assert.Equal(TimeSpan.Zero, at.Offset);
				Assert.Equal(new DateTimeOffset(2026, 11, 28, 10, 0, 0, TimeSpan.Zero), at);
			}

			[Fact]
			public void Keeps_Fractional_Seconds()
			{
				// Arrange
				var path = "/events/2026-11-28T10:00:00.1234567Z";

				// Act
				var result = MatchOne("/events/{at:datetime}", path);

				// Assert
				Assert.Equal(1234567, Value<DateTimeOffset>(result, "at").Ticks % TimeSpan.TicksPerSecond);
			}

			[Theory]
			[InlineData("2026-11-28 10:00:00")]
			[InlineData("2026-11-28T10:00")]
			public void Accepts_Other_Timestamp_Forms_As_Utc(string value)
			{
				// Arrange
				var path = $"/events/{value}";

				// Act
				var result = MatchOne("/events/{at:datetime}", path);

				// Assert
				Assert.Equal(new DateTimeOffset(2026, 11, 28, 10, 0, 0, TimeSpan.Zero), Value<DateTimeOffset>(result, "at"));
			}

			[Fact]
			public void Plain_Date_Is_Midnight_Utc()
			{
				// Arrange
				var path = "/events/2026-11-28";

				// Act
				var result = MatchOne("/events/{at:datetime}", path);

				// Assert
				Assert.Equal(new DateTimeOffset(2026, 11, 28, 0, 0, 0, TimeSpan.Zero), Value<DateTimeOffset>(result, "at"));
			}

			[Theory]
			[InlineData("2026-13-01T10:00:00Z")]
			[InlineData("2026-11-31T10:00:00Z")]
			[InlineData("not-a-date")]
			[InlineData("42")]
			public void Does_Not_Match_Invalid_Value(string value)
			{
				// Arrange
				var path = $"/events/{value}";

				// Act
				var result = MatchOne("/events/{at:datetime}", path);

				// Assert
				_ = Assert.IsType<NotFound>(result);
			}
		}

		public class TimeOnly
		{
			[Theory]
			[InlineData("10:30", 10, 30, 0)]
			[InlineData("10:30:15", 10, 30, 15)]
			[InlineData("00:00", 0, 0, 0)]
			public void Captures_Time(string value, int hour, int minute, int second)
			{
				// Arrange
				var path = $"/opens/{value}";

				// Act
				var result = MatchOne("/opens/{at:timeonly}", path);

				// Assert
				Assert.Equal(new System.TimeOnly(hour, minute, second), Value<System.TimeOnly>(result, "at"));
			}

			[Theory]
			[InlineData("2026-11-28")]
			[InlineData("12")]
			[InlineData("25:00")]
			[InlineData("10:60")]
			public void Does_Not_Match_Other_Values(string value)
			{
				// Arrange
				var path = $"/opens/{value}";

				// Act
				var result = MatchOne("/opens/{at:timeonly}", path);

				// Assert
				_ = Assert.IsType<NotFound>(result);
			}
		}

		public class Dates_Side_By_Side
		{
			[Theory]
			[InlineData(true)]
			[InlineData(false)]
			public void Date_Goes_To_DateOnly_And_Timestamp_To_DateTime_In_Any_Order(bool dateOnlyFirst)
			{
				// Arrange
				var router = new Router();
				var day = NewHandler();
				var at = NewHandler();
				if (dateOnlyFirst)
				{
					router.Map("/events/{day:dateonly}").Get(day);
					router.Map("/events/{at:datetime}").Get(at);
				}
				else
				{
					router.Map("/events/{at:datetime}").Get(at);
					router.Map("/events/{day:dateonly}").Get(day);
				}

				router.Lock(Sp, new ServiceCollection());

				// Act
				var date = router.Match(Method.Get, "/events/2026-11-28");
				var timestamp = router.Match(Method.Get, "/events/2026-11-28T10:00:00Z");

				// Assert
				Assert.Same(day, Assert.IsType<Matched>(date).Handler);
				Assert.Same(at, Assert.IsType<Matched>(timestamp).Handler);
			}
		}
	}
}
