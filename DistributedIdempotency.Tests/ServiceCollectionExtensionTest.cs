using DistributedIdempotency.Data;
using DistributedIdempotency.Logic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using IDistributedCache = DistributedIdempotency.Data.IDistributedCache;

namespace DistributedIdempotency.Tests;

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
        services.RegisterIdempotencyDependencies();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetService<IdempotencyService>());
        Assert.NotNull(serviceProvider.GetService<ILocalCache>());
        Assert.NotNull(serviceProvider.GetService<IDistributedCache>());
    }



    [Fact]
    public void RegisterIdempotencyDependencies_WithDistributedCache_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new Mock<IConfiguration>();
        configuration.SetupGet(c => c["DistributedIdempotency:StrictMode"]).Returns("false");

        // Act
        services.RegisterIdempotencyDependencies<CustomDistributedCache>();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetService<IdempotencyService>());
        Assert.NotNull(serviceProvider.GetService<ILocalCache>());
        Assert.NotNull(serviceProvider.GetService<IDistributedCache>());
    }
}

// Mock class for testing
class CustomDistributedCache : IDistributedCache
{
    public Task<bool> Contains(string key) => Task.FromResult(true);
    public Task<T> Get<T>(string key) => Task.FromResult<T>(default);

    public Task<bool> IsHealthy() => Task.FromResult(true);


    public Task Remove(string key) => Task.CompletedTask;

    public Task<T> Save<T>(string key, T response, DateTime expiry) => Task.FromResult<T>(default);

}



