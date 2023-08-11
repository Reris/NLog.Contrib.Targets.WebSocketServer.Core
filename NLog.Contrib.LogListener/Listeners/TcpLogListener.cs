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
        this.TcpOptions = options.Tcp;
    }

    private INetworkProviderFactory ProviderFactory { get; }
    private ILogClientFactory ClientFactory { get; }
    public IList<TcpListenerOptions> TcpOptions { get; }

    public void Start()
    {
        foreach (var tcpOption in this.TcpOptions)
        {
            TcpLogListener.Logger.Info("Starting {Ip}:{Port}…", tcpOption.Ip, tcpOption.Port);
            var listener = this.ProviderFactory.Create<ITcpNetworkListener>();
            listener.Connected += (_, e) => this.ClientFactory.CreateFor(e.Channel, tcpOption);
            var endpoint = new IPEndPoint(TcpLogListener.ParseIpAddress(tcpOption), tcpOption.Port);
            listener.Connect(endpoint);
        }
    }

    private static IPAddress ParseIpAddress(TcpListenerOptions tcpOption)
        => tcpOption.Ip.ToLowerInvariant() switch
        {
            "v4" => IPAddress.Any,
            "v6" => IPAddress.IPv6Any,
            _ => IPAddress.Parse(tcpOption.Ip)
        };
}
