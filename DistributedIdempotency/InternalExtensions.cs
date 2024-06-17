using DistributedIdempotency.Data;
using DistributedIdempotency.Logic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedIdempotency
{
    internal static class InternalExtensions
    {

        static bool IsJobRunning = false;
        internal static void StartCacheSync(this IServiceCollection services)
        {
            if (!IsJobRunning)
            {
                var provider = services.BuildServiceProvider();
                var memoryCache = provider.GetRequiredService<IMemoryCache>();
                var cache = provider.GetRequiredService<IDistributedCache>();
                IdempotencyCacheImpl.IsDistributedCacheHealthy = true;
                Task.Run(() => new IdempotencySyncJob(cache, new IdempotencyLocalCacheImpl(memoryCache)));
                IsJobRunning = true;
            }
        }

    }
}
