namespace NLog.Contrib.LogListener.Tcp.Data;

public record struct NLogMessage(NLogLevel Level, string Logger, string Message);
