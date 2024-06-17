using Microsoft.Extensions.DependencyInjection;
using DistributedIdempotency;
using DistributedIdempotency.Behaviours;
using DistributedIdempotency.Data;
using Moq;
using Microsoft.Extensions.Configuration;
using DistributedIdempotency.Logic;

namespace DistributedIdempotency.Tests
{
    public class ServiceCollectionExtensionTests
    {
        [Fact]
        public void RegisterIdempotencyDependencies_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new Mock<IConfiguration>();
            configuration.SetupGet(c => c["DistributedIdempotency:StrictMode"]).Returns("false");

            // Act
            services.RegisterIdempotencyDependencies(true);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            Assert.NotNull(serviceProvider.GetService<IdempotencyService>());
            Assert.NotNull(serviceProvider.GetService<IDistributedCache>());
            Assert.NotNull(serviceProvider.GetService<IdempotencyCache>());
            Assert.NotNull(serviceProvider.GetService<LocalCache>());
        }


        [Fact]
        public void RegisterIdempotencyDependencies_WithDistributedCache_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new Mock<IConfiguration>();
            configuration.SetupGet(c => c["DistributedIdempotency:StrictMode"]).Returns("false");

            // Act
            services.RegisterIdempotencyDependencies<CustomDistributedCache>(false);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            Assert.NotNull(serviceProvider.GetService<IdempotencyService>());
            Assert.NotNull(serviceProvider.GetService<IDistributedCache>());
            Assert.NotNull(serviceProvider.GetService<IdempotencyCache>());
            Assert.NotNull(serviceProvider.GetService<LocalCache>());
        }

        // Mock class for testing
        private class CustomDistributedCache : IDistributedCache
        {
            public byte[] Get(string key) => null;
            public Task<byte[]> GetAsync(string key, CancellationToken token = default) => Task.FromResult<byte[]>(null);
            public void Refresh(string key) { }
            public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
            public void Remove(string key) { }
            public Task RemoveAsync(string key, CancellationToken token = default) => Task.CompletedTask;
            public void Set(string key, byte[] value, DistributedCacheEntryOptions options) { }
            public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => Task.CompletedTask;
        }
    }
}
