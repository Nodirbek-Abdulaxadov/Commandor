using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Commandor.Tests;

// ── Direct-injection (Fusion-style) tests ───────────────────────────────────
// Verifies that injecting the service interface itself goes through the
// auto-generated cached proxy: queries are memoized, commands invalidate.

public class CachedProxyTests
{
    public record TodoItem(int Id, string Title);

    public interface ITodoService : ICommandorService
    {
        [QueryHandler(CacheTtlSeconds = 60)]
        Task<TodoItem?> GetByIdAsync(int id, CancellationToken ct = default);

        [QueryHandler]
        Task<List<TodoItem>> ListAsync(string? filter = null, CancellationToken ct = default);

        [CommandHandler]
        Task<TodoItem> CreateAsync(string title, CancellationToken ct = default);

        // Not decorated — must pass straight through to the impl.
        Task<int> CountUntrackedAsync(CancellationToken ct = default);
    }

    public class TodoService : ITodoService
    {
        public static int GetByIdCalls;
        public static int ListCalls;
        public static int CreateCalls;
        public static int UntrackedCalls;

        public static void Reset() => GetByIdCalls = ListCalls = CreateCalls = UntrackedCalls = 0;

        public Task<TodoItem?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            Interlocked.Increment(ref GetByIdCalls);
            return Task.FromResult<TodoItem?>(new TodoItem(id, $"Item-{id}"));
        }

        public Task<List<TodoItem>> ListAsync(string? filter = null, CancellationToken ct = default)
        {
            Interlocked.Increment(ref ListCalls);
            return Task.FromResult(new List<TodoItem> { new(1, $"{filter ?? "all"}-a"), new(2, $"{filter ?? "all"}-b") });
        }

        public Task<TodoItem> CreateAsync(string title, CancellationToken ct = default)
        {
            Interlocked.Increment(ref CreateCalls);
            return Task.FromResult(new TodoItem(99, title));
        }

        public Task<int> CountUntrackedAsync(CancellationToken ct = default)
        {
            Interlocked.Increment(ref UntrackedCalls);
            return Task.FromResult(42);
        }
    }

    private static IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddCommandor(typeof(CachedProxyTests).Assembly);
        services.AddCommandorService<ITodoService, TodoService>();
        TodoService.Reset();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Query_through_injected_service_is_cached()
    {
        var sp = BuildProvider();
        var svc = sp.GetRequiredService<ITodoService>();

        var a = await svc.GetByIdAsync(7);
        var b = await svc.GetByIdAsync(7);

        Assert.Equal(a, b);
        Assert.Equal(1, TodoService.GetByIdCalls);
    }

    [Fact]
    public async Task Query_with_different_args_caches_separately()
    {
        var sp = BuildProvider();
        var svc = sp.GetRequiredService<ITodoService>();

        await svc.GetByIdAsync(1);
        await svc.GetByIdAsync(2);
        await svc.GetByIdAsync(1);

        Assert.Equal(2, TodoService.GetByIdCalls);
    }

    [Fact]
    public async Task Command_through_injected_service_auto_invalidates()
    {
        var sp = BuildProvider();
        var svc = sp.GetRequiredService<ITodoService>();

        await svc.GetByIdAsync(5);
        Assert.Equal(1, TodoService.GetByIdCalls);

        await svc.CreateAsync("new");  // [CommandHandler] → auto-invalidate

        await svc.GetByIdAsync(5);
        Assert.Equal(2, TodoService.GetByIdCalls);  // cache was cleared by the command
    }

    [Fact]
    public async Task Default_parameter_value_is_preserved()
    {
        var sp = BuildProvider();
        var svc = sp.GetRequiredService<ITodoService>();

        var a = await svc.ListAsync();          // filter == null
        var b = await svc.ListAsync();          // same default → same cache entry
        var c = await svc.ListAsync("done");    // different filter → different entry

        Assert.Equal(2, TodoService.ListCalls);
        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public async Task Non_decorated_method_passes_straight_through()
    {
        var sp = BuildProvider();
        var svc = sp.GetRequiredService<ITodoService>();

        await svc.CountUntrackedAsync();
        await svc.CountUntrackedAsync();
        await svc.CountUntrackedAsync();

        // No caching applied — service is called on every invocation.
        Assert.Equal(3, TodoService.UntrackedCalls);
    }

    [Fact]
    public async Task Manual_Invalidate_still_works()
    {
        var sp = BuildProvider();
        var svc = sp.GetRequiredService<ITodoService>();
        var cmd = sp.GetRequiredService<ICommandor>();

        await svc.GetByIdAsync(3);
        Assert.Equal(1, TodoService.GetByIdCalls);

        await cmd.InvalidateAsync<ITodoService>();

        await svc.GetByIdAsync(3);
        Assert.Equal(2, TodoService.GetByIdCalls);
    }
}
