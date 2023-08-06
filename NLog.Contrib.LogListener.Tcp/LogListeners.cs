using System;
using Microsoft.Extensions.DependencyInjection;
using NLog.Contrib.LogListener.Tcp.Deserializers;
using NLog.Contrib.LogListener.Tcp.Listeners;

namespace NLog.Contrib.LogListener.Tcp;

public static class LogListeners
{
    public static void Start()
    {
    }

    public static IServiceCollection AddTcpLogListener(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddOptions<TcpLogListenerOptions>();
        serviceCollection.AddSingleton<ILogger>(LogManager.GetLogger("redistribute"));
        serviceCollection.AddSingleton<INLogDeserializer, NLogDeserializer>();
        serviceCollection.AddTransient<ITcpNetworkListener, TcpNetworkListener>();
        serviceCollection.AddSingleton<ILogListener, TcpLogListener>();
        serviceCollection.AddSingleton<ILogClientFactory, LogClientFactory>();
        serviceCollection.AddSingleton<IFormatDeserializer, JsonFormatDeserializer>();
        serviceCollection.AddSingleton<IFormatDeserializer, Log4JFormatDeserializer>();
        return serviceCollection;
    }

    public static void StartTcpLogListeners(this IServiceProvider serviceProvider)
    {
        foreach (var logListener in serviceProvider.GetServices<ILogListener>())
        {
            logListener.Start();
        }
    }
}
