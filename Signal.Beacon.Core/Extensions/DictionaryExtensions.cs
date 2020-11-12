using System.Collections.Generic;

namespace Signal.Beacon.Core.Extensions
{
    public static class DictionaryExtensions
    {
        public static void Append<TKey, TValue>(
            this IDictionary<TKey, ICollection<TValue>> @this, 
            TKey key,
            TValue value)
        {
            if (!@this.ContainsKey(key))
                @this.Add(key, new List<TValue> {value});
            else @this[key].Add(value);
        }

        public static void AddOrSet<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key, TValue value)
        {
            if (!@this.ContainsKey(key))
                @this.Add(key, value);
            else @this[key] = value;
        }
    }
}