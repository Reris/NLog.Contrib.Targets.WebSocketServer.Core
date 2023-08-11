using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NLog.Contrib.LogListener.Deserializers.Formats;

public static class DictionaryHelper<TKey, TValue>
{
    public static readonly Dictionary<TKey, TValue> Empty = new();

    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static TValue GetValueOrDefault(IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default!)
        => dictionary.Count > 0 ? dictionary.GetValueOrDefault(key, defaultValue) : defaultValue;

    public static void Rename(Dictionary<TKey, TValue> dictionary, TKey from, TKey to)
    {
        if (dictionary.Remove(from, out var value))
        {
            dictionary[to] = value;
        }
    }
}
