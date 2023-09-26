using System;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;

namespace NLog.Contrib.LogListener.Listeners;

public class NetworkProviderFactory : INetworkProviderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public NetworkProviderFactory(IServiceProvider serviceProvider)
    {
        this._serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public INetworkProvider Create(ProtocolType protocol)
        => protocol switch
        {
            ProtocolType.Tcp => this._serviceProvider.GetRequiredService<TcpNetworkProvider>(),
            _ => throw new NotSupportedException($"Protocol '{protocol}' is not supported")
        };
}
