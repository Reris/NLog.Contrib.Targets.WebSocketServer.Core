using System;
using System.Net;
using Microsoft.Extensions.Options;

namespace NLog.Contrib.LogListener.Tcp.Listeners;

public class TcpLogListener : ILogListener
{
    private static readonly ILogger Logger = InternalLogger.Get<TcpNetworkChannel>();
    
    public TcpLogListener(ITcpNetworkListener provider, ILogClientFactory clientFactory, IOptionsMonitor<TcpLogListenerOptions> optionsMonitor)
    {
        this.Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        this.ClientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));

        this.Provider.Connected += this.ProviderOnConnected;

        var options = optionsMonitor.CurrentValue;
        this.ListeningEndPoint = options.EndPoint;
    }

    private INetworkProvider Provider { get; }
    private ILogClientFactory ClientFactory { get; }
    public IPEndPoint ListeningEndPoint { get; }

    public void Start()
    {
        TcpLogListener.Logger.Info("Starting…");
        this.Provider.Connect(this.ListeningEndPoint);
    }

    private void ProviderOnConnected(object? sender, ConnectedEventArgs e) => this.ClientFactory.CreateFor(e.Channel);
}
