// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Microsoft.Extensions.Logging;
using Wrap;

namespace Merlin.Extensions;

public static partial class LoggerExtensions
{
	/// <summary>
	/// Log a failure at its own level, with its exception, message template and arguments,
	/// inside a scope named by its context (if it has one).
	/// </summary>
	/// <param name="logger">Logger.</param>
	/// <param name="failure">Failure to log.</param>
	public static void Log(this ILogger logger, Failure failure)
	{
		var level = failure.Level.ToMs();
		if (!logger.IsEnabled(level))
		{
			return;
		}

		using var _ = failure.Context is string ctx ? logger.BeginScope(ctx) : null;
#pragma warning disable CA1848, CA2254 // template comes from the Failure, by design
		logger.Log(level, failure.Exception, failure.Message, failure.Args);
#pragma warning restore CA1848, CA2254
	}
}
