using DistributedIdempotency.Data;
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

        private async Task SyncWithDistributedCacheAsync(string idempotencyKey)
        {
            Console.WriteLine($"Syncing idempotency key '{idempotencyKey}' with distributed cache...");
            var value = await LocalCache.Get<IdempotentResponse>(idempotencyKey);
            if (value?.IsProcessing == false) return;
            var response = await DistributedCache.Get<IdempotentResponse>(idempotencyKey);
            if (response?.IsProcessing == true)
            {
                Requests.Enqueue(idempotencyKey);
                return;
            }

            await LocalCache.Save(idempotencyKey, response, response.Expiry);

        }
    }

}
