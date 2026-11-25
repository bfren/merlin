// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using MsLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Merlin.Extensions;

internal static partial class LogLevelExtensions
{
	/// <summary>
	/// Convert a Wrap log level to the equivalent Microsoft log level.
	/// </summary>
	/// <remarks>
	/// Wrap's levels are bit flags and Microsoft's are sequential, so the values must be mapped, never cast.
	/// Unknown levels map to <see cref="MsLevel.Warning"/>.
	/// </remarks>
	/// <param name="this">Wrap log level.</param>
	/// <returns>The equivalent Microsoft log level.</returns>
	internal static MsLevel ToMs(this Wrap.Logging.LogLevel @this) =>
		@this switch
		{
			Wrap.Logging.LogLevel.Verbose =>
				MsLevel.Trace,

			Wrap.Logging.LogLevel.Debug =>
				MsLevel.Debug,

			Wrap.Logging.LogLevel.Information =>
				MsLevel.Information,

			Wrap.Logging.LogLevel.Error =>
				MsLevel.Error,

			Wrap.Logging.LogLevel.Fatal => MsLevel.Critical,
			_ => MsLevel.Warning
		};
}
