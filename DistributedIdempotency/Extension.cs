using DistributedIdempotency.Behaviours;
using DistributedIdempotency.Data;
using DistributedIdempotency.Logic;
using Microsoft.Extensions.DependencyInjection;
namespace DistributedIdempotency
{
    public static class Extension
    {
        static bool IsJobRunning = false;
        public static void RegisterIdempotencyDependencies(this IServiceCollection services, IDistributedCache cache, bool useStrictMode = true)
        {
            services.AddScoped<ILocalCache, IdempotencyLocalCacheImpl>();
            services.AddScoped<IdempotencyCache, IdempotencyCacheImpl>();
            services.AddScoped<IdempotencyService, IdempotencyServiceImpl>();
            services.AddScoped<IdempotencyInterceptor>();
            IdempotencyCacheImpl.SetSharedCache(cache);
            if (!IsJobRunning)
            {
                Task.Run(() => new IdempotencySyncJob(cache, new IdempotencyLocalCacheImpl()).Sync(null));
                IsJobRunning = true;
            }
        }

    }
}
