using Commandor;
using Commandor.Generated;
using Microsoft.Extensions.DependencyInjection;

namespace Commandor.Tests;

/// <summary>
/// Caching behaviour against the v3 query surface: every read goes
/// through <c>AppCommandor.SomeService.Method(...)</c>; second call to
/// the same args returns the cached result without re-hitting the
/// implementation.
/// </summary>
public class CachingTests
{
    private static (AppCommandor Commandor, IServiceProvider Sp) BuildPrimitive()
    {
        var services = new ServiceCollection();
        services.AddCommandor(typeof(CachingTests).Assembly);
        services.AddCommandorService<IPrimitiveService, PrimitiveService>();
        services.AddAppCommandor();
        PrimitiveService.Reset();
        var sp = services.BuildServiceProvider();
        return (sp.GetRequiredService<AppCommandor>(), sp);
    }

    [Fact]
    public async Task Same_args_returns_cached_result()
    {
        var (cmd, _) = BuildPrimitive();

        var r1 = await cmd.PrimitiveService.Search(10, "hello");
        var r2 = await cmd.PrimitiveService.Search(10, "hello");

        Assert.Equal(r1, r2);
        Assert.Equal(1, PrimitiveService.CallCount);
    }

    [Fact]
    public async Task Different_args_do_not_share_cache()
    {
        var (cmd, _) = BuildPrimitive();

        var r1 = await cmd.PrimitiveService.Search(1, "x");
        var r2 = await cmd.PrimitiveService.Search(2, "x");

        Assert.NotEqual(r1, r2);
        Assert.Equal(2, PrimitiveService.CallCount);
    }

    [Fact]
    public async Task Invalidate_clears_cache()
    {
        var (cmd, _) = BuildPrimitive();

        await cmd.PrimitiveService.Search(5, "a");
        Assert.Equal(1, PrimitiveService.CallCount);

        cmd.Invalidate<IPrimitiveService>();
        await cmd.PrimitiveService.Search(5, "a");

        Assert.Equal(2, PrimitiveService.CallCount);
    }

    [Fact]
    public async Task InvalidateAsync_clears_cache()
    {
        var (cmd, _) = BuildPrimitive();

        await cmd.PrimitiveService.Search(7, "b");
        Assert.Equal(1, PrimitiveService.CallCount);

        await cmd.InvalidateAsync<IPrimitiveService>();
        await cmd.PrimitiveService.Search(7, "b");

        Assert.Equal(2, PrimitiveService.CallCount);
    }

    [Fact]
    public async Task Record_param_caches_by_value()
    {
        var (cmd, _) = BuildPrimitive();

        var filter1 = new SearchFilter("active", 1);
        var filter2 = new SearchFilter("active", 1);  // same value, different instance

        var r1 = await cmd.PrimitiveService.SearchWithFilter(filter1);
        var r2 = await cmd.PrimitiveService.SearchWithFilter(filter2); // hits cache

        Assert.Equal(r1, r2);
        Assert.Equal(1, PrimitiveService.FilterCallCount);
    }

    public record SearchFilter(string Status, int Page);

    public interface IPrimitiveService : ICommandorService
    {
        [QueryHandler]
        Task<string> Search(int id, string keyword, CancellationToken ct = default);

        [QueryHandler]
        Task<string> SearchWithFilter(SearchFilter filter, CancellationToken ct = default);
    }

    public class PrimitiveService : IPrimitiveService
    {
        public static int CallCount;
        public static int FilterCallCount;

        public static void Reset()
        {
            CallCount = 0;
            FilterCallCount = 0;
        }

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
