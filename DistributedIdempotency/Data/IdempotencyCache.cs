using DistributedIdempotency.Exceptions;
using DistributedIdempotency.Helpers;
using DistributedIdempotency.Logic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DistributedIdempotency.Data
{
    public interface IDistributedCache
    {
        Task<T> Get<T>(string key);
        Task<bool> Contains(string key);
        Task<T> Save<T>(string key, T response, DateTime expiry);
        Task Remove(string key);
        Task<bool> IsHealthy();
    }
    public interface ILocalCache
    {
        T? Get<T>(string key);
        bool Contains(string key);
        T Save<T>(string key, T response, DateTime expiry);
        void Remove(string key);
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

        public bool Contains(string key)
        {
            Logger.Debug($"Checking local cache for key: {key}");
            return Cache.TryGetValue(key, out var value);
        }

        public T? Get<T>(string key)
        {

            Logger.Debug($"Retrieving item from local cache for key: {key}");
            bool isFound = Cache.TryGetValue(key, out object? item);
            if (isFound) return (T)item;
            return default;

        }

        public T Save<T>(string key, T response, DateTime expiry)
        {
            Logger.Debug($"Saving item to local cache for key: {key}");
            return Cache.Set(key, response, expiry);
        }

        public void Remove(string key)
        {
            Logger.Debug($"Removing item from local cache for key: {key}");
            Cache.Remove(key);
        }


    }
    internal class IdempotencyStubDistributedCache : IDistributedCache
    {
        public Task<bool> Contains(string key)
        {
            Logger.Debug($"Checking mock cache for key: {key}");
            return Task.FromResult(false);
        }

        public Task<T> Get<T>(string key)
        {
            Logger.Debug($"Retrieving item from mock cache for key: {key}");
            return Task.FromResult<T>(default);
        }

        public Task<bool> IsHealthy()
        {
            return Task.FromResult(true);
        }

        public Task Remove(string key)
        {
            Logger.Debug($"Removing item from mock cache for key: {key}");
            return Task.CompletedTask;
        }

        public Task<T> Save<T>(string key, T response, DateTime expiry)
        {
            Logger.Debug($"Saving item to mock cache for key: {key}");
            return Task.FromResult<T>(default);
        }
    }
    internal class IdempotencyCacheImpl : IdempotencyCache
    {
        readonly IDistributedCache DistributedCache;
        readonly ILocalCache LocalCache;
        public static bool IsDistributedCacheHealthy;
        readonly Configuration? _config;
        bool SkipDistributedCacheCheck => !IsDistributedCacheHealthy && !_config.StrictMode;

        public IdempotencyCacheImpl(ILocalCache localCache, IDistributedCache distributedCache, IOptions<Configuration> options)
        {
            LocalCache = localCache;
            DistributedCache = distributedCache;
            _config = options.Value ?? new Configuration();
        }
        public async Task<IdempotentResponse> Get(string key)
        {
            var foundInLocal = LocalCache.Contains(key);
            if (foundInLocal)
            {
                var item = LocalCache.Get<IdempotentResponse>(key);
                return item;
            }
            if (SkipDistributedCacheCheck) return null;
            ValidateHealth();
            if (await DistributedCache.Contains(key))
            {
                Logger.Debug($"Retrieving item from distributed cache for key: {key}");
                return await DistributedCache.Get<IdempotentResponse>(key);
            }
            return null;
        }
        void ValidateHealth()
        {
            if (!IsDistributedCacheHealthy) throw new CacheException("Cache is currently unhealthy");
        }
        public async Task<bool> Contains(string key)
        {
            var foundInLocal = LocalCache.Contains(key);
            if (SkipDistributedCacheCheck) return foundInLocal;
            ValidateHealth();
            return foundInLocal || (await DistributedCache.Contains(key));
        }
        public async Task<IdempotentResponse> Save(string key, IdempotentResponse response)
        {

            LocalCache.Save(key, response, response.Expiry);

            if (SkipDistributedCacheCheck) return LocalCache.Get<IdempotentResponse>(key);

            ValidateHealth();
            Logger.Debug($"Saving item to distributed cache for key: {key}");
            await DistributedCache.Save(key, response, response.Expiry);

            return LocalCache.Get<IdempotentResponse>(key);
        }

    }
}
