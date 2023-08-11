namespace NLog.Contrib.LogListener.Data;

public record struct NLogMessage(NLogLevel Level, string Logger, string Message);
