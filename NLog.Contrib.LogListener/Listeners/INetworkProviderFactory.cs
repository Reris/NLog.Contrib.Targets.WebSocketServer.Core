using System.Net.Sockets;

namespace NLog.Contrib.LogListener.Listeners;

public interface INetworkProviderFactory
{
    INetworkProvider Create(ProtocolType protocol);
}
