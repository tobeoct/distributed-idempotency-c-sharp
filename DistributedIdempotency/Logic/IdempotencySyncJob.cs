using DistributedIdempotency.Data;
using System.Collections.Concurrent;

namespace DistributedIdempotency.Logic
{
    public class IdempotencySyncJob
    {
        private readonly ConcurrentQueue<string> Requests = new ConcurrentQueue<string>();

        readonly IDistributedCache SharedCache;
        readonly ILocalCache LocalCache;

        public IdempotencySyncJob(IDistributedCache cache, ILocalCache localCache)
        {
            SharedCache = cache;
            LocalCache = localCache;
            IdempotencyServiceImpl.SyncRequest += OnSyncRequested;
            _ = new Timer(Sync, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        private void OnSyncRequested(object sender, string idempotencyKey)
        {
            // Log the duplicate idempotency key in the queue
            Requests.Enqueue(idempotencyKey);
        }

        public void Sync(object state)
        {
            var count = Requests.Count;
            while (count > 0)
            {
                if (Requests.TryDequeue(out string idempotencyKey))
                {
                    // Process the duplicate idempotency key (e.g., sync with shared cache)
                    SyncWithSharedCacheAsync(idempotencyKey).Wait();
                }
                count -= 1;
            }
        }

        private async Task SyncWithSharedCacheAsync(string idempotencyKey)
        {
            // Logic to sync the duplicate idempotency key with the shared cache
            Console.WriteLine($"Syncing idempotency key '{idempotencyKey}' with shared cache...");

            var response = await SharedCache.Get<IdempotentResponse>(idempotencyKey);

            if (response == null)
            {
                Requests.Enqueue(idempotencyKey);
                return;
            }

            await LocalCache.Save(idempotencyKey, response, response.Expiry);

        }
    }

}
