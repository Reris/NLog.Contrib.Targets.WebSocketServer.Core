using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Options;

namespace NLog.Contrib.LogListener.Listeners;

public class TcpLogListener : ILogListener
{
    private static readonly ILogger Logger = InternalLogger.Get<TcpNetworkChannel>();

    public TcpLogListener(INetworkProviderFactory providerFactory, ILogClientFactory clientFactory, IOptionsMonitor<LogListenerOptions> optionsMonitor)
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
            TcpLogListener.Logger.Info("Starting {Ip}:{Port}…", options.Ip, options.Port);
            var listener = this.ProviderFactory.Create<ITcpNetworkListener>();
            listener.Connected += (_, e) => this.ClientFactory.CreateFor(e.Channel, options);
            var endpoint = new IPEndPoint(TcpLogListener.ParseIpAddress(options), options.Port);
            listener.Connect(endpoint);
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
