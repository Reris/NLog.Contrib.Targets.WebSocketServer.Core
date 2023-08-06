namespace NLog.Contrib.LogListener.Tcp.Listeners;

public interface ILogClientFactory
{
    ILogClient CreateFor(INetworkChannel channel);
}
