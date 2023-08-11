using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NLog.Contrib.LogListener.Deserializers;
using NLog.Contrib.LogListener.Deserializers.Formats;
using NLog.Contrib.LogListener.Listeners;

namespace NLog.Contrib.LogListener;

public static class LogListeners
{
    public static IServiceCollection AddLogListener(this IServiceCollection serviceCollection, IConfiguration optionsSection)
    {
        serviceCollection.Configure<LogListenerOptions>(optionsSection);
        serviceCollection.AddLogListener();
        return serviceCollection;
    }

    public static IServiceCollection AddLogListener(this IServiceCollection serviceCollection, Action<LogListenerOptions>? options = null)
    {
        if (options is not null)
        {
            serviceCollection.Configure(options);
        }

        serviceCollection.AddSingleton<ILogger>(LogManager.GetLogger("redistribute"));
        serviceCollection.AddSingleton<IDeserializerFactory, DeserializerFactory>();
        serviceCollection.AddSingleton<IFormat, JsonFormat>();
        serviceCollection.AddSingleton<IFormat, Log4JXmlFormat>();

        serviceCollection.AddSingleton<INetworkProviderFactory, NetworkProviderFactory>();
        serviceCollection.AddTransient<ITcpNetworkListener, TcpNetworkListener>();
        serviceCollection.AddSingleton<ILogListener, TcpLogListener>();
        serviceCollection.AddSingleton<ILogClientFactory, LogClientFactory>();
        return serviceCollection;
    }

    public static void StartLogListeners(this IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<LogListenerOptions>>();
        InternalLogger.Enabled = options.CurrentValue.LogInternals;
        options.OnChange(o => InternalLogger.Enabled = o.LogInternals);

        foreach (var logListener in serviceProvider.GetServices<ILogListener>())
        {
            logListener.Start();
        }
    }
}
