using DistributedIdempotency.Logic;
using System.Collections.Concurrent;

namespace DistributedIdempotency.Data
{
    internal static class IdempotencyCache
    {
        static ConcurrentDictionary<string, IdempotentResponse> Cache = new ConcurrentDictionary<string, IdempotentResponse>();
        public static IdempotentResponse Get(string key)
        {
            if (Cache.ContainsKey(key)) return Cache[key];
            return null;
        }
        public static bool Contains(string key)
        {
            return Cache.ContainsKey(key);
        }
        public static IdempotentResponse Save(string key, IdempotentResponse response)
        {
            if (Cache.ContainsKey(key))
            {
                Cache[key] = response;
                return Cache[key];
            }
            Cache.TryAdd(key, response);
            return Cache[key];
        }
    }
}
