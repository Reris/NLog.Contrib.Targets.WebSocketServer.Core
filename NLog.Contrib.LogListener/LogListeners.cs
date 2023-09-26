using System;
using System.Linq;
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
        serviceCollection.Configure<LogListenerOptions>(BindLogListenerOptions);
        serviceCollection.AddLogListener();
        return serviceCollection;

        void BindLogListenerOptions(LogListenerOptions options)
        {
            optionsSection.Bind(options);
            var registeredFormats = RegisteredFormats.GetRegisteredFormats(serviceCollection);
            for (var i = 0; i < options.Listeners.Count; i++)
            {
                var tcp = options.Listeners[i];
                var formatsSection = optionsSection.GetSection($"Listeners:{i}:Formats");
                foreach (var formatOptionsSection in formatsSection.GetChildren())
                {
                    var typeName = formatOptionsSection.GetValue<string>("Type");
                    var optionsType = registeredFormats.Formats.FirstOrDefault(a => a.Discriminator == typeName).OptionsType;
                    if (optionsType is not null)
                    {
                        var formatOptions = (FormatOptions)Activator.CreateInstance(optionsType);
                        formatOptionsSection.Bind(formatOptions);
                        tcp.Formats.Add(formatOptions);
                    }
                }
            }
        }
    }

    public static IServiceCollection AddLogListener(this IServiceCollection serviceCollection, Action<LogListenerOptions>? options = null)
    {
        if (options is not null)
        {
            serviceCollection.Configure(options);
        }

        serviceCollection.AddSingleton<ILogger>(LogManager.GetLogger("redistribute"));
        serviceCollection.AddSingleton<IDeserializerFactory, DeserializerFactory>();
        serviceCollection.AddTransient<JsonFormat>();
        serviceCollection.AddTransient<Log4JXmlFormat>();
        serviceCollection.AddTransient<Log4NetXmlFormat>();

        serviceCollection.AddSingleton<INetworkProviderFactory, NetworkProviderFactory>();
        serviceCollection.AddTransient<TcpNetworkProvider>();
        serviceCollection.AddSingleton<ILogListener, Listeners.LogListener>();
        serviceCollection.AddSingleton<ILogClientFactory, LogClientFactory>();
        serviceCollection.AddSingleton<RegisteredFormats>(_ => RegisteredFormats.GetRegisteredFormats(serviceCollection));

        return serviceCollection;
    }

    public static void StartLogListeners(this IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<LogListenerOptions>>();
        LogListeners.ConfigureListeners(serviceProvider, options.CurrentValue);
        options.OnChange(o => LogListeners.ConfigureListeners(serviceProvider, o));

        foreach (var logListener in serviceProvider.GetServices<ILogListener>())
        {
            logListener.Start();
        }
    }

    private static void ConfigureListeners(IServiceProvider serviceProvider, LogListenerOptions options)
    {
        InternalLogger.Enabled = options.LogInternals;

        var factory = serviceProvider.GetRequiredService<IDeserializerFactory>();
        foreach (var listenerOptions in options.Listeners)
        {
            factory.Configure(listenerOptions);
        }
    }
}
