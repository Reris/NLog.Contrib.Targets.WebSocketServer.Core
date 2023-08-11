namespace NLog.Contrib.LogListener;

public static class InternalLogger
{
    private static readonly NullLogger NullLogger = new(LogManager.LogFactory);
    public static bool Enabled { get; set; } = false;

    public static ILogger Get<TFor>() => InternalLogger.Enabled
                                             ? LogManager.GetLogger($"{typeof(TFor).Namespace}.{typeof(TFor).Name}")
                                             : InternalLogger.NullLogger;
}
