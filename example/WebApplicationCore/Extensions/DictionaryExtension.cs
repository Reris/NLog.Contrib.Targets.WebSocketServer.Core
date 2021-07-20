using System.Collections.Generic;

namespace WebApplicationCore.Extensions
{
    public static class DictionaryExtension
    {
        internal static T Get<T>(this IDictionary<string, object> dictionary, string key)
        {
            object item;
            if (dictionary.TryGetValue(key, out item))
            {
                return (T)item;
            }

            return default(T);
        }
    }
}
