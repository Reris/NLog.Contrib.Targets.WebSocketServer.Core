﻿using System;
using NLog.Contrib.LogListener.Deserializers;

namespace NLog.Contrib.LogListener.Listeners;

public class LogClientFactory : ILogClientFactory
{
    private readonly ILogger _clientLogger;
    private readonly IDeserializerFactory _deserializerFactory;

    public LogClientFactory(ILogger clientLogger, IDeserializerFactory deserializerFactory)
    {
        this._clientLogger = clientLogger ?? throw new ArgumentNullException(nameof(clientLogger));
        this._deserializerFactory = deserializerFactory ?? throw new ArgumentNullException(nameof(deserializerFactory));
    }

    public ILogClient CreateFor(INetworkChannel channel, ListenerOptions options)
        => options switch
        {
            not null => new NLogClient(
                channel ?? throw new ArgumentNullException(nameof(channel)),
                this._clientLogger,
                this._deserializerFactory.Get<INLogDeserializer>(options),
                options),
            _ => throw new NotSupportedException()
        };
}
