// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using MsLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Merlin.Extensions;

internal static class LogLevelExtensions
{
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
