using DistributedIdempotency.Logic;
using System.Collections.Concurrent;

namespace DistributedIdempotency.Data
{
    public interface ICache
    {
        Task<T> Get<T>(string key);
        Task<bool> Contains(string key);
        Task<T> Save<T>(string key, T response, DateTime expiry);
        Task Remove(string key);
    }
    public interface IDistributedCache : ICache
    {
    }
    public interface ILocalCache : ICache
    {
    }
    internal interface IdempotencyCache
    {
        Task<IdempotentResponse> Get(string key);
        Task<bool> Contains(string key);
        Task<IdempotentResponse> Save(string key, IdempotentResponse response);
    }
    internal class IdempotencyLocalCacheImpl : ILocalCache
    {
        class CacheItem
        {
            public DateTime Expiry { get; set; }
            public object Value { get; set; }
        }
        static ConcurrentDictionary<string, CacheItem> Cache = new ConcurrentDictionary<string, CacheItem>();

        public Task<bool> Contains(string key)
        {
            return Task.Run(() => Cache.ContainsKey(key));
        }

        public Task<T> Get<T>(string key)
        {
            if (Cache.ContainsKey(key) && DateTime.Now < Cache[key].Expiry) return Task.Run(() => (T)Cache[key].Value);
            return default;
        }

        public Task<T> Save<T>(string key, T response, DateTime expiry)
        {
            if (Cache.ContainsKey(key))
            {
                Cache[key] = new CacheItem() { Value = response, Expiry = expiry };
            }
            else
            {
                Cache.TryAdd(key, new CacheItem() { Value = response, Expiry = expiry });
            }
            return Task.Run(() => response);
        }

        public Task Remove(string key)
        {
            Cache.TryRemove(key, out var item);
            return Task.CompletedTask;
        }
    }

    internal class IdempotencyCacheImpl : IdempotencyCache
    {
        static IDistributedCache SharedCache;
        static ILocalCache LocalCache;

        public static void SetSharedCache(IDistributedCache cache)
        {
            SharedCache = cache;
        }
        public IdempotencyCacheImpl(ILocalCache cache)
        {
            LocalCache = cache;
        }
        public async Task<IdempotentResponse> Get(string key)
        {
            if (await LocalCache.Contains(key)) return await LocalCache.Get<IdempotentResponse>(key);
            if (await SharedCache?.Contains(key)) return await SharedCache.Get<IdempotentResponse>(key);
            return null;
        }
        public async Task<bool> Contains(string key)
        {
            return await LocalCache.Contains(key) || (await SharedCache?.Contains(key));
        }
        public async Task<IdempotentResponse> Save(string key, IdempotentResponse response)
        {

            await LocalCache.Save(key, response, response.Expiry);

            await SharedCache.Save(key, response, response.Expiry);

            return await LocalCache.Get<IdempotentResponse>(key);
        }

    }
}
