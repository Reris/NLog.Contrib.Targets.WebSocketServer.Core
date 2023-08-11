using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NLog.Contrib.LogListener.Deserializers.Formats;

namespace NLog.Contrib.LogListener.Deserializers;

public class DeserializerFactory : IDeserializerFactory
{
    private readonly IEnumerable<IFormat> _formats;
    private IReadOnlyList<CachedEntry> _cache = new List<CachedEntry>(0);

    public DeserializerFactory(IEnumerable<IFormat> formats)
    {
        this._formats = formats;
    }

    public T Get<T>(DeserializerOptions options)
        where T : IDeserializer
    {
        var formatsKey = string.Join(';', options.Formats);
        if (this._cache.FirstOrDefault(a => a.Requested == typeof(T) && a.Formats == formatsKey).Deserializer is { } found)
        {
            return (T)found;
        }

        return this.GetCached<T>(options, formatsKey);
    }

    private T GetCached<T>(DeserializerOptions options, string formatsKey)
        where T : IDeserializer
    {
        var newCache = new List<CachedEntry>(this._cache.Count + 1);
        newCache.AddRange(this._cache);
        var formats = options.Formats
                             .Select(
                                 a => this._formats.FirstOrDefault(b => b.Name.Equals(a, StringComparison.OrdinalIgnoreCase))
                                      ?? throw new UnknownFormatException(a))
                             .ToArray();
        var deserializer = (T)this.Create<T>(options, formats);
        newCache.Add(new CachedEntry(typeof(T), formatsKey, deserializer));
        this._cache = newCache;
        return deserializer;
    }

    [PublicAPI]
    public virtual IDeserializer Create<T>(DeserializerOptions options, IFormat[] formats)
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

    private record struct CachedEntry(Type Requested, string Formats, IDeserializer Deserializer);
}
