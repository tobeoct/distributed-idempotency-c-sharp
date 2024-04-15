using DistributedIdempotency.Behaviours;
using DistributedIdempotency.Logic;
using Microsoft.Extensions.DependencyInjection;
namespace DistributedIdempotency
{
    public static class Extension
    {
        public static void RegisterIdempotencyDependencies(this IServiceCollection services)
        {
            services.AddScoped<IdempotencyService, IdempotencyServiceImpl>();
            services.AddScoped<IdempotencyInterceptor>();


            Task.Run(() => new IdempotencySyncJob().Sync(null));
        }
    }
}
