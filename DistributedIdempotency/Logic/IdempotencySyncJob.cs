using DistributedIdempotency.Data;
using DistributedIdempotency.Helpers;
using System.Collections.Concurrent;

namespace DistributedIdempotency.Logic
{
    public class IdempotencySyncJob
    {
        private readonly ConcurrentQueue<string> Requests = new();

        readonly IDistributedCache DistributedCache;
        readonly ILocalCache LocalCache;
        public IdempotencySyncJob(IDistributedCache distributedCache, ILocalCache localCache)
        {
            DistributedCache = distributedCache;
            LocalCache = localCache;
            IdempotencyServiceImpl.SyncRequest += OnSyncRequested;
            _ = new Timer(Sync, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            _ = new Timer(CacheHealthCheck, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        private void OnSyncRequested(object sender, string idempotencyKey)
        {
            // Log the duplicate idempotency key in the queue
            Requests.Enqueue(idempotencyKey);
        }

        private void Sync(object state)
        {
            var count = Requests.Count;
            while (count > 0)
            {
                if (Requests.TryDequeue(out string idempotencyKey))
                {
                    SyncWithDistributedCacheAsync(idempotencyKey).Wait();
                }
                count -= 1;
            }
        }

        private void CacheHealthCheck(object state)
        {
            IdempotencyCacheImpl.IsDistributedCacheHealthy = DistributedCache.IsHealthy().Result;
        }
        private async Task SyncWithDistributedCacheAsync(string idempotencyKey)
        {
            Logger.Debug($"Syncing idempotency key '{idempotencyKey}' with distributed cache...");
            var value = LocalCache.Get<IdempotentResponse>(idempotencyKey);
            if (value?.IsProcessing == false) return;
            var response = await DistributedCache.Get<IdempotentResponse>(idempotencyKey);
            if (response?.IsProcessing == true)
            {
                Requests.Enqueue(idempotencyKey);
                return;
            }

            LocalCache.Save(idempotencyKey, response, response.Expiry);

        }
    }

}
