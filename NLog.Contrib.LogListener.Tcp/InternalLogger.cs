namespace NLog.Contrib.LogListener.Tcp;

public static class InternalLogger
{
    public static ILogger Get<TFor>() => LogManager.GetLogger($"internal.{typeof(TFor).Namespace}.{typeof(TFor).Name}");
}
