using DistributedIdempotency.Data;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DistributedIdempotency.Logic
{
    public class IdempotentResponse
    {
        public IdempotentResponse()
        {

        }
        public IdempotentResponse(string key, DateTime expiry)
        {

            Key = key;
            Expiry = expiry;
        }
        public IdempotentResponse(string key, DateTime expiry, object? response, int? statusCode, bool isProcessing)
        {
            Key = key;
            Expiry = expiry;
            Response = response;
            IsProcessing = isProcessing;
            StatusCode = statusCode;
        }
        public string Key { get; init; }
        public DateTime Expiry { get; init; }
        public object? Response { get; init; }
        public int? StatusCode { get; init; }
        public bool IsProcessing { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.Now;
    }
    public interface IdempotencyService
    {
        Task<bool> CheckForDuplicateAsync(string key);
        Task<IdempotentResponse> GetResponseAsync(string key, int timeoutInMilliseconds);
        Task<IdempotentResponse> UpsertAsync(string key, IActionResult payload = null, bool isProcessing = true, int window = 30000);

    }
    internal class IdempotencyServiceImpl(IdempotencyCache cache) : IdempotencyService
    {
        IdempotencyCache Cache = cache;

        internal static event EventHandler<string> SyncRequest;

        public async Task<bool> CheckForDuplicateAsync(string key)
        {
            var sw = Stopwatch.StartNew();
            var duplicateFound = await Cache.Contains(key);
            if (duplicateFound) OnSyncRequested(key);
            sw.Stop();
            Console.WriteLine($"Benchmark (CheckForDuplicateAsync): <{sw.ElapsedMilliseconds}ms>");
            return duplicateFound;
        }
        public async Task<IdempotentResponse> GetResponseAsync(string key, int timeoutInMilliseconds)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var startTime = DateTime.Now;

                while (await CheckForInitialResponseAsync(key, startTime, timeoutInMilliseconds))
                {
                    Thread.Sleep(100);
                    Console.WriteLine($"Checking for duplicate's response for idempotency key '{key}' ...");
                }
                return await Cache.Get(key);
            }
            finally
            {
                sw.Stop();
                Console.WriteLine($"Benchmark(GetResponseAsync): <{sw.ElapsedMilliseconds}ms>");
            }
        }

        async Task<bool> CheckForInitialResponseAsync(string key, DateTime startTime, int timeoutInMilliseconds)
        {
            var cache = await Cache.Get(key);
            var isInitialTransactionStillProcessing = (cache?.IsProcessing ?? false);// && cache?.Response == null;
            var isExpired = DateTime.Now < cache?.Expiry;
            var isWithinDuplicateWindow = DateTime.Now < startTime.AddMilliseconds(timeoutInMilliseconds);
            return isInitialTransactionStillProcessing && isWithinDuplicateWindow && isExpired;
        }
        public async Task<IdempotentResponse> UpsertAsync(string key, IActionResult result = null, bool isProcessing = true, int window = 30000)
        {
            var sw = Stopwatch.StartNew();
            IdempotentResponse idempotentResponse;
            (object?, int?) cachedResponse = (null, null);

            if (result is ObjectResult objectResult)
            {
                var resultValue = objectResult.Value;
                cachedResponse = (resultValue, objectResult.StatusCode);
            }
            if (result is StatusCodeResult statusCodeResult)
            {
                cachedResponse.Item2 = statusCodeResult.StatusCode;
            }


            if (!await Cache.Contains(key))
            {
                idempotentResponse = new IdempotentResponse(key, DateTime.Now.AddMilliseconds(window), cachedResponse.Item1, cachedResponse.Item2, isProcessing);
                await Cache.Save(key, idempotentResponse);
                OnSyncRequested(key);
                return idempotentResponse;
            }

            idempotentResponse = await Cache.Get(key);
            idempotentResponse = new IdempotentResponse(key, idempotentResponse.Expiry, cachedResponse.Item1, cachedResponse.Item2, isProcessing);
            await Cache.Save(key, idempotentResponse);
            sw.Stop();
            Console.WriteLine($"Benchmark(UpsertAsync): <{sw.ElapsedMilliseconds}ms>");
            return idempotentResponse;
        }


        private void OnSyncRequested(string idempotencyKey)
        {
            SyncRequest?.Invoke(this, idempotencyKey);
        }
    }
}
