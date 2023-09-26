using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Options;

namespace NLog.Contrib.LogListener.Listeners;

public class LogListener : ILogListener
{
    private static readonly ILogger Logger = InternalLogger.Get<LogListener>();

    public LogListener(INetworkProviderFactory providerFactory, ILogClientFactory clientFactory, IOptionsMonitor<LogListenerOptions> optionsMonitor)
    {
        this.ProviderFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
        this.ClientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));

        var options = optionsMonitor.CurrentValue;
        this.Options = options.Listeners;
    }

    private INetworkProviderFactory ProviderFactory { get; }
    private ILogClientFactory ClientFactory { get; }
    public IList<ListenerOptions> Options { get; }

    public void Start()
    {
        foreach (var options in this.Options)
        {
            LogListener.Logger.Info("Starting {Ip}:{Port}…", options.Ip, options.Port);
            var provider = this.ProviderFactory.Create(options.Protocol);
            provider.Connected += (_, e) => this.ClientFactory.CreateFor(e.Channel, options);
            var endpoint = new IPEndPoint(LogListener.ParseIpAddress(options), options.Port);
            provider.Connect(endpoint);
        }
    }

    private static IPAddress ParseIpAddress(ListenerOptions option)
        => option.Ip.ToLowerInvariant() switch
        {
            "v4" => IPAddress.Any,
            "v6" => IPAddress.IPv6Any,
            _ => IPAddress.Parse(option.Ip)
        };
}
