using DistributedIdempotency.Behaviours;
using DistributedIdempotency.Data;
using DistributedIdempotency.Helpers;
using DistributedIdempotency.Logic;
using Microsoft.Extensions.DependencyInjection;
namespace DistributedIdempotency
{
    public static class Extension
    {
        public static void RegisterIdempotencyDependencies(this IServiceCollection services, bool useStrictMode = true)
        {
            Configuration.StrictMode = useStrictMode;
            services.AddSingleton<ILocalCache, IdempotencyLocalCacheImpl>();
            services.AddSingleton<IdempotencyCache, IdempotencyCacheImpl>();
            services.AddScoped<IdempotencyService, IdempotencyServiceImpl>();
            services.AddScoped<IdempotencyInterceptor>();
            services.AddMemoryCache();
            if (!services.Any(s => s.ServiceType == typeof(IDistributedCache)))
            {
                services.AddSingleton<IDistributedCache, IdempotencyStubDistributedCache>();
            }

            services.StartCacheSync();
        }

        public static void RegisterIdempotencyDependencies<TDistributedCache>(this IServiceCollection services, bool useStrictMode = true) where TDistributedCache : class, IDistributedCache
        {
            if (!services.Any(s => s.ServiceType == typeof(IDistributedCache)))
            {
                _ = services.AddSingleton<IDistributedCache, TDistributedCache>();
            }
            services.RegisterIdempotencyDependencies(useStrictMode);
        }

        public static void AddDistributedIdempotencyCache<TDistributedCache>(this IServiceCollection services) where TDistributedCache : class, IDistributedCache
        {
            if (!services.Any(s => s.ServiceType == typeof(IDistributedCache)))
            {
                _ = services.AddSingleton<IDistributedCache, TDistributedCache>();
            }
        }

    }
}
