using DistributedIdempotency.Behaviours;
using System.Collections.Concurrent;

namespace DistributedIdempotency.Logic
{
    public class IdempotencySyncJob
    {
        private ConcurrentQueue<string> requests = new ConcurrentQueue<string>();
        Timer timer;

        public IdempotencySyncJob()
        {
            IdempotencyServiceImpl.SyncRequest += OnSyncRequested;
            timer = new Timer(Sync, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        private void OnSyncRequested(object sender, string idempotencyKey)
        {
            // Log the duplicate idempotency key in the queue
            requests.Enqueue(idempotencyKey);
        }

        public void Sync(object state)
        {
            var count = requests.Count;
            while (count>0)
            {
                if (requests.TryDequeue(out string idempotencyKey))
                {
                    // Process the duplicate idempotency key (e.g., sync with shared cache)
                    SyncWithSharedCache(idempotencyKey);
                }
                count -= 1;
            }
        }

        private void SyncWithSharedCache(string idempotencyKey)
        {
            // Logic to sync the duplicate idempotency key with the shared cache
            Console.WriteLine($"Syncing idempotency key '{idempotencyKey}' with shared cache...");
            requests.Enqueue(idempotencyKey);
        }
    }

}
