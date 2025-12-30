using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Commandor;

namespace Commandor.Tests;

public class CachingTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICommandor _commandor;
    private readonly ITestService _service;

    public CachingTests()
    {
        var services = new ServiceCollection();
        services.AddCommandor(typeof(CachingTests).Assembly);
        services.AddCommandorService<ITestService, TestService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _commandor = _serviceProvider.GetRequiredService<ICommandor>();
        _service = _serviceProvider.GetRequiredService<ITestService>();
        
        TestService.Reset();
    }

    [Fact]
    public async Task SendAsync_ShouldCacheResult()
    {
        // Arrange
        var query = new GetDataQuery(1);

        // Act
        var result1 = await _commandor.SendAsync(query);
        var result2 = await _commandor.SendAsync(query);

        // Assert
        Assert.Equal(result1, result2);
        Assert.Equal(1, TestService.CallCount);
    }

    [Fact]
    public async Task Invalidate_ShouldClearCache()
    {
        // Arrange
        var query = new GetDataQuery(1);
        await _commandor.SendAsync(query);
        Assert.Equal(1, TestService.CallCount);

        // Act
        _commandor.Invalidate<ITestService>();
        await _commandor.SendAsync(query);

        // Assert
        Assert.Equal(2, TestService.CallCount);
    }
    
    [Fact]
    public async Task InvalidateAsync_ShouldClearCache()
    {
        // Arrange
        var query = new GetDataQuery(1);
        await _commandor.SendAsync(query);
        Assert.Equal(1, TestService.CallCount);

        // Act
        await _commandor.InvalidateAsync<ITestService>();
        await _commandor.SendAsync(query);

        // Assert
        Assert.Equal(2, TestService.CallCount);
    }

    // --- Helpers ---

    public record GetDataQuery(int Id) : IRequest<string>;

    public interface ITestService : ICommandorService
    {
        [QueryHandler(CacheTtlSeconds = 60)]
        Task<string> GetData(GetDataQuery query, CancellationToken ct = default);
    }

    public class TestService : ITestService
    {
        public static int CallCount = 0;

        public static void Reset() => CallCount = 0;

        public Task<string> GetData(GetDataQuery query, CancellationToken ct = default)
        {
            Interlocked.Increment(ref CallCount);
            return Task.FromResult($"Result-{query.Id}-{Guid.NewGuid()}");
        }
    }
}
