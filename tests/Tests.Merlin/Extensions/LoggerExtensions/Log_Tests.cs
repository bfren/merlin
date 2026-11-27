// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Microsoft.Extensions.Logging;
using Wrap;
using WrapLevel = Wrap.Logging.LogLevel;

namespace Merlin.Extensions.LoggerExtensions_Tests;

public class Log_Tests
{
	/// <summary>
	/// Records what is logged, and the scopes open when each entry is written.
	/// </summary>
	private sealed class TestLogger(LogLevel minimum = LogLevel.Trace) : ILogger
	{
		public sealed record class Entry(
			LogLevel Level,
			Exception? Exception,
			string Message,
			IReadOnlyList<KeyValuePair<string, object?>> State,
			IReadOnlyList<object> Scopes
		);

		private readonly List<object> open = [];

		public List<Entry> Entries { get; } = [];

		public List<object> Scopes { get; } = [];

		public bool IsEnabled(LogLevel logLevel) =>
			logLevel >= minimum;

		public IDisposable BeginScope<TState>(TState state) where TState : notnull
		{
			Scopes.Add(state);
			open.Add(state);
			return new Scope(() => open.Remove(state));
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
			Entries.Add(new(
				logLevel,
				exception,
				formatter(state, exception),
				state as IReadOnlyList<KeyValuePair<string, object?>> ?? [],
				[.. open]
			));

		private sealed class Scope(Action dispose) : IDisposable
		{
			public void Dispose() => dispose();
		}
	}

	public class Level
	{
		[Theory]
		[InlineData(WrapLevel.Verbose, LogLevel.Trace)]
		[InlineData(WrapLevel.Information, LogLevel.Information)]
		[InlineData(WrapLevel.Error, LogLevel.Error)]
		[InlineData(WrapLevel.Fatal, LogLevel.Critical)]
		public void Logs_At_Mapped_Level(WrapLevel level, LogLevel expected)
		{
			// Arrange
			var logger = new TestLogger();
			var failure = R.Fail(Rnd.Str).WithLevel(level);

			// Act
			logger.Log(failure);

			// Assert
			Assert.Equal(expected, Assert.Single(logger.Entries).Level);
		}

		[Fact]
		public void Skips_Disabled_Level()
		{
			// Arrange
			var logger = new TestLogger(LogLevel.Error);
			var failure = R.Fail(Rnd.Str).WithLevel(WrapLevel.Information).WithContext(Rnd.Str, Rnd.Str);

			// Act
			logger.Log(failure);

			// Assert
			Assert.Empty(logger.Entries);
			Assert.Empty(logger.Scopes);
		}

		[Fact]
		public void Logs_Enabled_Level()
		{
			// Arrange
			var logger = new TestLogger(LogLevel.Error);
			var failure = R.Fail(Rnd.Str).WithLevel(WrapLevel.Fatal);

			// Act
			logger.Log(failure);

			// Assert
			Assert.Single(logger.Entries);
		}
	}

	public class Message
	{
		[Fact]
		public void Logs_Message()
		{
			// Arrange
			var logger = new TestLogger();
			var message = Rnd.Str;

			// Act
			logger.Log(R.Fail(message));

			// Assert
			Assert.Equal(message, Assert.Single(logger.Entries).Message);
		}

		[Fact]
		public void Formats_Template_With_Args()
		{
			// Arrange
			var logger = new TestLogger();
			var name = Rnd.Str;
			var number = Rnd.Int;

			// Act
			logger.Log(R.Fail("Value {Name} is not {Number}.", name, number));

			// Assert
			var entry = Assert.Single(logger.Entries);
			Assert.Equal($"Value {name} is not {number}.", entry.Message);
		}

		[Fact]
		public void Passes_Args_As_Structured_Values()
		{
			// Arrange
			var logger = new TestLogger();
			var name = Rnd.Str;
			var number = Rnd.Int;

			// Act
			logger.Log(R.Fail("Value {Name} is not {Number}.", name, number));

			// Assert
			var state = Assert.Single(logger.Entries).State;
			Assert.Contains(new("Name", name), state);
			Assert.Contains(new("Number", number), state);
			Assert.Contains(new("{OriginalFormat}", "Value {Name} is not {Number}."), state);
		}
	}

	public class Exceptions
	{
		[Fact]
		public void Passes_Exception()
		{
			// Arrange
			var logger = new TestLogger();
			var exception = new InvalidOperationException(Rnd.Str);

			// Act
			logger.Log(new Failure(exception));

			// Assert
			Assert.Same(exception, Assert.Single(logger.Entries).Exception);
		}

		[Fact]
		public void Passes_Null_Without_Exception()
		{
			// Arrange
			var logger = new TestLogger();

			// Act
			logger.Log(R.Fail(Rnd.Str));

			// Assert
			Assert.Null(Assert.Single(logger.Entries).Exception);
		}
	}

	public class Scopes
	{
		[Fact]
		public void Logs_Inside_Scope_Named_By_Context()
		{
			// Arrange
			var logger = new TestLogger();
			var failure = R.Fail(Rnd.Str).WithContext(Rnd.Str, Rnd.Str);

			// Act
			logger.Log(failure);

			// Assert
			Assert.NotNull(failure.Context);
			var entry = Assert.Single(logger.Entries);
			Assert.Equal(failure.Context, Assert.Single(entry.Scopes));
		}

		[Fact]
		public void Closes_Scope_After_Logging()
		{
			// Arrange
			var logger = new TestLogger();
			var failure = R.Fail(Rnd.Str).WithContext(Rnd.Str, Rnd.Str);

			// Act
			logger.Log(failure);
			logger.Log(R.Fail(Rnd.Str));

			// Assert
			Assert.Empty(logger.Entries[1].Scopes);
		}

		[Fact]
		public void Does_Not_Begin_Scope_Without_Context()
		{
			// Arrange
			var logger = new TestLogger();

			// Act
			logger.Log(R.Fail(Rnd.Str));

			// Assert
			Assert.Empty(logger.Scopes);
		}
	}
}
