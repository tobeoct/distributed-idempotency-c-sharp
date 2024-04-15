using DistributedIdempotency.Data;
using Microsoft.AspNetCore.Mvc;

namespace DistributedIdempotency.Logic
{
   
    public record IdempotentResponse(string Key, DateTime Expiry, IActionResult Response = null, bool IsProcessing = true);
    public interface IdempotencyService
    {
        bool CheckForDuplicate(string key);
        IdempotentResponse GetResponse(string key, int timeoutInMilliseconds);
        IdempotentResponse Upsert(string key, IActionResult payload = null, bool isProcessing = true, int window = 30000);
        
    }
    public class IdempotencyServiceImpl : IdempotencyService
    {
        public static event EventHandler<string> SyncRequest;
        public bool CheckForDuplicate(string key)
        {
            var duplicateFound =  IdempotencyCache.Contains(key);
            if (duplicateFound) OnSyncRequested(key);
            return duplicateFound;
        }
        public IdempotentResponse GetResponse(string key, int timeoutInMilliseconds)
        {
            var startTime = DateTime.Now;                                                                                                                                                                                                                                                                                                                         

            while (IdempotencyCache.Contains(key) && IdempotencyCache.Get(key).IsProcessing && IdempotencyCache.Get(key).Response == null && DateTime.Now < startTime.AddMilliseconds(timeoutInMilliseconds) && DateTime.Now < IdempotencyCache.Get(key).Expiry)
            {
                Console.WriteLine($"Checking for duplicate for idempotency key '{key}' ...");
            }
            return IdempotencyCache.Get(key);
        }
        public IdempotentResponse Upsert(string key, IActionResult payload = null, bool isProcessing = true, int window = 30000)
        {
            IdempotentResponse idempotentResponse;
            if (!IdempotencyCache.Contains(key))
            {
                idempotentResponse = new IdempotentResponse(key, DateTime.Now.AddMilliseconds(window), payload, isProcessing);
                IdempotencyCache.Save(key, idempotentResponse);
                OnSyncRequested(key);
                return idempotentResponse;
            }

            idempotentResponse = IdempotencyCache.Get(key);
            idempotentResponse = new IdempotentResponse(key, idempotentResponse.Expiry, payload, isProcessing);
            IdempotencyCache.Save(key, idempotentResponse);
            return idempotentResponse;
        }


        private void OnSyncRequested(string idempotencyKey)
        {
            SyncRequest?.Invoke(this, idempotencyKey);
        }
    }
}
