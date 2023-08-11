using System;
using Microsoft.Extensions.DependencyInjection;

namespace NLog.Contrib.LogListener.Listeners;

public class NetworkProviderFactory : INetworkProviderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public NetworkProviderFactory(IServiceProvider serviceProvider)
    {
        this._serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public T Create<T>()
        where T : INetworkProvider
        => this._serviceProvider.GetRequiredService<T>();
}