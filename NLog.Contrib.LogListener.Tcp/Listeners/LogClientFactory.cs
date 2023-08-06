using System;
using NLog.Contrib.LogListener.Tcp.Deserializers;

namespace NLog.Contrib.LogListener.Tcp.Listeners;

public class LogClientFactory : ILogClientFactory
{
    private readonly ILogger _clientLogger;
    private readonly INLogDeserializer _deserializer;

    public LogClientFactory(ILogger clientLogger, INLogDeserializer deserializer)
    {
        this._clientLogger = clientLogger ?? throw new ArgumentNullException(nameof(clientLogger));
        this._deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
    }

    public ILogClient CreateFor(INetworkChannel channel)
        => new NLogClient(
            channel ?? throw new ArgumentNullException(nameof(channel)),
            this._clientLogger,
            this._deserializer);
}
