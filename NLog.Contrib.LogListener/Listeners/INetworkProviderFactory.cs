namespace NLog.Contrib.LogListener.Listeners;

public interface INetworkProviderFactory
{
    T Create<T>()
        where T : INetworkProvider;
}
