namespace NLog.Contrib.LogListener.Listeners;

public interface ILogClientFactory
{
    ILogClient CreateFor(INetworkChannel channel, ListenerOptions options);
}
