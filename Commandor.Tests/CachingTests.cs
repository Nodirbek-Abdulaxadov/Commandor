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

    // ── IRequest-based (existing) ──────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_ShouldCacheResult()
    {
        var query = new GetDataQuery(1);
        var result1 = await _commandor.SendAsync(query);
        var result2 = await _commandor.SendAsync(query);
        Assert.Equal(result1, result2);
        Assert.Equal(1, TestService.CallCount);
    }

    [Fact]
    public async Task Invalidate_ShouldClearCache()
    {
        var query = new GetDataQuery(1);
        await _commandor.SendAsync(query);
        Assert.Equal(1, TestService.CallCount);
        _commandor.Invalidate<ITestService>();
        await _commandor.SendAsync(query);
        Assert.Equal(2, TestService.CallCount);
    }

    [Fact]
    public async Task InvalidateAsync_ShouldClearCache()
    {
        var query = new GetDataQuery(1);
        await _commandor.SendAsync(query);
        Assert.Equal(1, TestService.CallCount);
        await _commandor.InvalidateAsync<ITestService>();
        await _commandor.SendAsync(query);
        Assert.Equal(2, TestService.CallCount);
    }

    // ── GetAsync alias ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_ShouldBehaveSameAsSendAsync()
    {
        var query = new GetDataQuery(42);
        var r1 = await _commandor.GetAsync(query);
        var r2 = await _commandor.GetAsync(query);  // served from cache
        Assert.Equal(r1, r2);
        Assert.Equal(1, TestService.CallCount);     // service called only once
    }

    [Fact]
    public async Task GetAsync_AndSendAsync_ShareSameCache()
    {
        var query = new GetDataQuery(7);
        var fromSend = await _commandor.SendAsync(query);
        var fromGet  = await _commandor.GetAsync(query);  // cache hit
        Assert.Equal(fromSend, fromGet);
        Assert.Equal(1, TestService.CallCount);           // still one call
    }

    // ── Plain-type mode (no IRequest wrapper) ─────────────────────────────────

    private IServiceProvider BuildPlainTypeProvider()
    {
        var services = new ServiceCollection();
        services.AddCommandor(typeof(CachingTests).Assembly);
        services.AddCommandorService<IPlainTypeService, PlainTypeService>();
        PlainTypeService.Reset();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task PlainType_SameArgs_ShouldReturnCachedResult()
    {
        var sp = BuildPlainTypeProvider();
        var cmd = sp.GetRequiredService<ICommandor>();

        // Two calls with identical primitive arguments → one service call
        var r1 = await cmd.CachingTestsPlainTypeService.Search(id: 10, keyword: "hello");
        var r2 = await cmd.CachingTestsPlainTypeService.Search(id: 10, keyword: "hello");

        Assert.Equal(r1, r2);
        Assert.Equal(1, PlainTypeService.CallCount);
    }

    [Fact]
    public async Task PlainType_DifferentArgs_ShouldNotShareCache()
    {
        var sp = BuildPlainTypeProvider();
        var cmd = sp.GetRequiredService<ICommandor>();

        // Different id → different cache entry
        var r1 = await cmd.CachingTestsPlainTypeService.Search(id: 1, keyword: "x");
        var r2 = await cmd.CachingTestsPlainTypeService.Search(id: 2, keyword: "x");

        Assert.NotEqual(r1, r2);
        Assert.Equal(2, PlainTypeService.CallCount);
    }

    [Fact]
    public async Task PlainType_Invalidate_ShouldClearCache()
    {
        var sp = BuildPlainTypeProvider();
        var cmd = sp.GetRequiredService<ICommandor>();

        await cmd.CachingTestsPlainTypeService.Search(id: 5, keyword: "a");
        Assert.Equal(1, PlainTypeService.CallCount);

        cmd.Invalidate<IPlainTypeService>();
        await cmd.CachingTestsPlainTypeService.Search(id: 5, keyword: "a");  // cache was cleared

        Assert.Equal(2, PlainTypeService.CallCount);
    }

    [Fact]
    public async Task PlainType_ComplexParam_ShouldCacheByHashCode()
    {
        var sp = BuildPlainTypeProvider();
        var cmd = sp.GetRequiredService<ICommandor>();

        var filter1 = new SearchFilter("active", 1);
        var filter2 = new SearchFilter("active", 1);  // same value, different instance

        var r1 = await cmd.CachingTestsPlainTypeService.SearchWithFilter(filter1);
        var r2 = await cmd.CachingTestsPlainTypeService.SearchWithFilter(filter2); // should hit cache

        Assert.Equal(r1, r2);
        Assert.Equal(1, PlainTypeService.FilterCallCount);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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

    // Plain-type service — no IRequest records needed.
    public interface IPlainTypeService : ICommandorService
    {
        [QueryHandler]
        Task<string> Search(int id, string keyword, CancellationToken ct = default);

        [QueryHandler]
        Task<string> SearchWithFilter(SearchFilter filter, CancellationToken ct = default);
    }

    // SearchFilter is a plain record — GetHashCode() is value-based, so caching works correctly.
    public record SearchFilter(string Status, int Page);

    public class PlainTypeService : IPlainTypeService
    {
        public static int CallCount = 0;
        public static int FilterCallCount = 0;
        public static void Reset() { CallCount = 0; FilterCallCount = 0; }

        public Task<string> Search(int id, string keyword, CancellationToken ct = default)
        {
            Interlocked.Increment(ref CallCount);
            return Task.FromResult($"Search-{id}-{keyword}-{Guid.NewGuid()}");
        }

        public Task<string> SearchWithFilter(SearchFilter filter, CancellationToken ct = default)
        {
            Interlocked.Increment(ref FilterCallCount);
            return Task.FromResult($"Filter-{filter.Status}-{filter.Page}-{Guid.NewGuid()}");
        }
    }
}
