using DistributedIdempotency.Logic;
using Microsoft.Extensions.Caching.Memory;
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
        IMemoryCache Cache;
        public IdempotencyLocalCacheImpl(IMemoryCache cache)
        {
            Cache = cache;
        }

        public Task<bool> Contains(string key)
        {
            return Task.Run(() => Cache.TryGetValue(key, out var value));
        }

        public Task<T?> Get<T>(string key)
        {
            return Task.Run(() =>
             {
                 bool isFound = Cache.TryGetValue(key, out object? item);
                 if (isFound) return (T)item;
                 return default;
             });
        }

        public Task<T> Save<T>(string key, T response, DateTime expiry)
        {
            return Task.Run(() => Cache.Set(key, response, expiry));
        }

        public Task Remove(string key)
        {
            Cache.Remove(key);
            return Task.CompletedTask;
        }


    }
    internal class IdempotencyStubDistributedCache : IDistributedCache
    {
        public Task<bool> Contains(string key)
        {
            return Task.FromResult(false);
        }

        public Task<T> Get<T>(string key)
        {
            return Task.FromResult<T>(default);
        }

        public Task Remove(string key)
        {
            return Task.CompletedTask;
        }

        public Task<T> Save<T>(string key, T response, DateTime expiry)
        {
            return Task.FromResult<T>(default);
        }
    }
    internal class IdempotencyCacheImpl : IdempotencyCache
    {
        static IDistributedCache DistributedCache;
        static ILocalCache LocalCache;

        public IdempotencyCacheImpl(ILocalCache localCache, IDistributedCache distributedCache)
        {
            LocalCache = localCache;
            DistributedCache = distributedCache;
        }
        public async Task<IdempotentResponse> Get(string key)
        {
            if (await LocalCache.Contains(key)) return await LocalCache.Get<IdempotentResponse>(key);
            if (await DistributedCache.Contains(key)) return await DistributedCache.Get<IdempotentResponse>(key);
            return null;
        }
        public async Task<bool> Contains(string key)
        {
            return await LocalCache.Contains(key) || (await DistributedCache.Contains(key));
        }
        public async Task<IdempotentResponse> Save(string key, IdempotentResponse response)
        {

            await LocalCache.Save(key, response, response.Expiry);

            await DistributedCache.Save(key, response, response.Expiry);

            return await LocalCache.Get<IdempotentResponse>(key);
        }

    }
}
