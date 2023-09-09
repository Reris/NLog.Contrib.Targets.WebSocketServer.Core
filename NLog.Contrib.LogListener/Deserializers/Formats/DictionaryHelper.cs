using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NLog.Contrib.LogListener.Deserializers.Formats;

public static class DictionaryHelper
{
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static TValue GetValueOrDefault<TKey, TValue>(
        this IReadOnlyDictionary<TKey, TValue> dictionary,
        IEnumerable<TKey> keys,
        TValue defaultValue = default!)
    {
        if (dictionary.Count > 0)
        {
            foreach (var key in keys)
            {
                if (dictionary.TryGetValue(key, out var found))
                {
                    return found;
                }
            }
        }

        return defaultValue;
    }

    public static void RenameKey<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey from, TKey to)
    {
        if (dictionary.Remove(from, out var value))
        {
            dictionary[to] = value;
        }
    }
}

public static class DictionaryHelper<TKey, TValue>
{
    public static readonly Dictionary<TKey, TValue> Empty = new();
}
