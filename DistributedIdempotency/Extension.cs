using DistributedIdempotency.Behaviours;
using DistributedIdempotency.Data;
using DistributedIdempotency.Helpers;
using DistributedIdempotency.Logic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DistributedIdempotency
{
    public static class ServiceCollectionExtension
    {
        public static void RegisterIdempotencyDependencies(this IServiceCollection services)
        {
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

        public static void RegisterIdempotencyDependencies<TDistributedCache>(this IServiceCollection services) where TDistributedCache : class, IDistributedCache
        {

            services.AddDistributedIdempotencyCache<TDistributedCache>();

            services.RegisterIdempotencyDependencies();
        }

        public static void AddDistributedIdempotencyCache<TDistributedCache>(this IServiceCollection services) where TDistributedCache : class, IDistributedCache
        {
            services.RemoveService<IDistributedCache>();
            _ = services.AddSingleton<IDistributedCache, TDistributedCache>();
        }

        public static void RemoveService<T>(this IServiceCollection services)
        {
            var serviceDescriptor = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(T));
            if (serviceDescriptor != null)
            {
                services.Remove(serviceDescriptor);
            }
        }
    }
}
