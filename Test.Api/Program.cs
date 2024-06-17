
using DistributedIdempotency;
using Microsoft.Extensions.Caching.Distributed;
using Distributed = Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using IDistributedCache = DistributedIdempotency.Data.IDistributedCache;
using DistributedIdempotency.Behaviours;
using DistributedIdempotency.Data;
using Microsoft.Extensions.Configuration;

namespace Test.Api
{

    public class RedisCache(Distributed.IDistributedCache cache) : IDistributedCache
    {
        Distributed.IDistributedCache Cache = cache;
        public async Task<bool> Contains(string key)
        {
            key = MapKey(key);
            return (await Get<object>(key)) != null;
        }

        private string MapKey(string key)
        {
            return "TEST-" + key;
        }
        public async Task<T> Get<T>(string key)
        {
            if (string.IsNullOrEmpty(key)) return default(T);
            key = MapKey(key);
            var jsonData = await Cache.GetStringAsync(key);

            if (jsonData is null)
            {
                return default(T);
            }
            return JsonSerializer.Deserialize<T>(jsonData);
        }

        public async Task<T> Save<T>(string key, T value, DateTime expiry)
        {
            if (string.IsNullOrEmpty(key)) return value;
            key = MapKey(key);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = expiry
            };

            var jsonData = JsonSerializer.Serialize(value);
            await Cache.SetStringAsync(key, jsonData, options);
            return value;
        }



        public async Task Remove(string key)
        {
            try
            {
                if (string.IsNullOrEmpty(key)) return;
                key = MapKey(key);
                await Cache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> IsHealthy()
        {
            try
            {
                var testKey = "HealthCheckKey";
                var testValue = "HealthCheckValue";

                await Cache.SetStringAsync(testKey, testValue, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                });

                var value = await Cache.GetStringAsync(testKey);

                return value == testValue;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var configuration = new ConfigurationBuilder()
                              .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                              .AddEnvironmentVariables()
                              .AddCommandLine(args)
                              .Build();
            // Add services to the container.

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<IdempotencyInterceptor>();
            });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("Redis");
            });

            builder.Services.RegisterIdempotencyDependencies<RedisCache>();

            var app = builder.Build();


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
