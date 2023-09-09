using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using NLog.Contrib.LogListener.Deserializers.Formats;

namespace NLog.Contrib.LogListener.Deserializers;

public class DeserializerFactory : IDeserializerFactory
{
    private readonly RegisteredFormats _registeredFormats;
    private readonly IServiceProvider _serviceProvider;
    private IReadOnlyList<DeserializerCacheEntry> _deserializerCache = new List<DeserializerCacheEntry>(0);
    private IReadOnlyList<FormatCacheEntry> _formatCache = new List<FormatCacheEntry>(0);

    public DeserializerFactory(IServiceProvider serviceProvider, RegisteredFormats registeredFormats)
    {
        this._serviceProvider = serviceProvider;
        this._registeredFormats = registeredFormats;
    }

    public T Get<T>(ListenerOptions options)
        where T : IDeserializer
    {
        var listenerKey = new ListenerKey(options);
        if (this._deserializerCache.FirstOrDefault(a => a.Key.Equals(listenerKey)).Deserializer is { } found)
        {
            return (T)found;
        }

        return this.CreateCached<T>(options, listenerKey);
    }

    public void Configure(ListenerOptions options)
    {
        if (options.Formats.Count == 0)
        {
            throw new ArgumentException($"No 'Formats' specified in '{options.Ip}:{options.Port}'");
        }

        var listenerKey = new ListenerKey(options);
        var formats = this._formatCache.FirstOrDefault(a => a.Key.Equals(listenerKey)).Formats ?? this.CreateFormatsCached(options, listenerKey, false);

        foreach (var formatOptions in options.Formats)
        {
            var discriminator = formatOptions.GetDiscriminator();
            var format = formats.FirstOrDefault(a => a.GetDiscriminator() == discriminator);
            (format as IConfigurable)?.Configure(formatOptions);
        }
    }

    public IFormat[] GetFormats(ListenerOptions options)
    {
        var listenerKey = new ListenerKey(options);
        if (this._formatCache.FirstOrDefault(a => a.Key.Equals(listenerKey)).Formats is { } found)
        {
            return found;
        }

        return this.CreateFormatsCached(options, listenerKey, true);
    }

    private IFormat[] CreateFormatsCached(ListenerOptions options, ListenerKey listenerKey, bool configure)
    {
        var newCache = new List<FormatCacheEntry>(this._formatCache.Count + 1);
        newCache.AddRange(this._formatCache);
        var formats = options.Formats
                             .Select(
                                 a =>
                                 {
                                     var discriminator = a.GetDiscriminator();
                                     var format = this.CreateFormat(discriminator);
                                     if (configure)
                                     {
                                         (format as IConfigurable)?.Configure(a);
                                     }

                                     return format;
                                 })
                             .ToArray();
        newCache.Add(new FormatCacheEntry(listenerKey, formats));
        this._formatCache = newCache;
        return formats;
    }

    private IFormat CreateFormat(string discriminator)
    {
        var formatType = this._registeredFormats.Formats.FirstOrDefault(a => a.Discriminator == discriminator).FormatType
                         ?? throw new UnknownFormatException(discriminator);
        return (IFormat)this._serviceProvider.GetRequiredService(formatType);
    }

    private T CreateCached<T>(ListenerOptions options, ListenerKey listenerKey)
        where T : IDeserializer
    {
        var newCache = new List<DeserializerCacheEntry>(this._deserializerCache.Count + 1);
        newCache.AddRange(this._deserializerCache);
        var formats = this.GetFormats(options);
        var deserializer = (T)this.Create<T>(formats);
        newCache.Add(new DeserializerCacheEntry(listenerKey, deserializer));
        this._deserializerCache = newCache;
        return deserializer;
    }

    protected virtual IDeserializer Create<T>(IFormat[] formats)
        where T : IDeserializer
        => typeof(T).Name switch
        {
            nameof(INLogDeserializer) => new NLogDeserializer(formats),
            _ => throw new NotSupportedException()
        };

    public class UnknownFormatException : Exception
    {
        public UnknownFormatException(string format)
            : base($"'{format}' is not a known {nameof(IFormat)}")
        {
        }
    }

    private record struct FormatCacheEntry(ListenerKey Key, IFormat[] Formats);

    private record struct DeserializerCacheEntry(ListenerKey Key, IDeserializer Deserializer);

    [SuppressMessage("ReSharper", "NotAccessedField.Local", Justification = "Combined Key")]
    private struct ListenerKey
    {
        private readonly ProtocolType _protocol;
        private readonly string _ip;
        private readonly int _port;

        public ListenerKey(ListenerOptions options)
        {
            this._protocol = options.Protocol;
            this._ip = options.Ip;
            this._port = options.Port;
        }
    }
}
