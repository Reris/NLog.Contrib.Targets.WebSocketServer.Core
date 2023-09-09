using System.Collections.Generic;

namespace NLog.Contrib.LogListener.Deserializers.Formats;

public static class FormatHelper
{
    public static LogLevel ParseLogLevel(string? level)
        => level?.ToLower() switch
        {
            "trace" => LogLevel.Trace,
            "verbose" => LogLevel.Trace,
            "debug" => LogLevel.Debug,
            "info" => LogLevel.Info,
            "information" => LogLevel.Info,
            "warn" => LogLevel.Warn,
            "warning" => LogLevel.Warn,
            "error" => LogLevel.Error,
            "fatal" => LogLevel.Fatal,
            _ => throw new KeyNotFoundException($"Unknown log level: {level}")
        };
}
