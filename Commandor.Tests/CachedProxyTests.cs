using Commandor.Generated;
using Microsoft.Extensions.DependencyInjection;

namespace Commandor.Tests;

/// <summary>
/// Verifies the v3 access pattern:
///   - Queries go through <c>AppCommandor.TodoService</c> and hit the
///     generated cached proxy.
///   - Commands go through <c>SendAsync</c> + a hand-written handler.
///   - The handler calls <c>commandor.Invalidate&lt;TService&gt;()</c>
///     so the next read repopulates the cache.
/// </summary>
public class CachedProxyTests
{
    public record TodoItem(int Id, string Title);

    public interface ITodoService : ICommandorService
    {
        [QueryHandler(CacheTtlSeconds = 60)]
        Task<TodoItem?> GetByIdAsync(int id, CancellationToken ct = default);

        [QueryHandler]
        Task<List<TodoItem>> ListAsync(string? filter = null, CancellationToken ct = default);

        // Not decorated — must pass straight through to the impl.
        Task<int> CountUntrackedAsync(CancellationToken ct = default);
    }

    public class TodoService : ITodoService
    {
        public static int GetByIdCalls;
        public static int ListCalls;
        public static int UntrackedCalls;

        public static void Reset() => GetByIdCalls = ListCalls = UntrackedCalls = 0;

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

        public Task<int> CountUntrackedAsync(CancellationToken ct = default)
        {
            Interlocked.Increment(ref UntrackedCalls);
            return Task.FromResult(42);
        }
    }

    public record CreateTodoCommand(string Title) : IRequest<TodoItem>;

    public sealed class CreateTodoHandler(ICommandor commandor) : IRequestHandler<CreateTodoCommand, TodoItem>
    {
        public Task<TodoItem> HandleAsync(CreateTodoCommand command, CancellationToken ct = default)
        {
            var todo = new TodoItem(99, command.Title);
            commandor.Invalidate<ITodoService>();
            return Task.FromResult(todo);
        }
    }

    private static AppCommandor BuildAppCommandor()
    {
        var services = new ServiceCollection();
        services.AddCommandor(typeof(CachedProxyTests).Assembly);
        services.AddCommandorService<ITodoService, TodoService>();
        services.AddAppCommandor();
        TodoService.Reset();
        return services.BuildServiceProvider().GetRequiredService<AppCommandor>();
    }

    [Fact]
    public async Task Query_through_service_property_is_cached()
    {
        var commandor = BuildAppCommandor();

        var a = await commandor.TodoService.GetByIdAsync(7);
        var b = await commandor.TodoService.GetByIdAsync(7);

        Assert.Equal(a, b);
        Assert.Equal(1, TodoService.GetByIdCalls);
    }

    [Fact]
    public async Task Query_with_different_args_caches_separately()
    {
        var commandor = BuildAppCommandor();

        await commandor.TodoService.GetByIdAsync(1);
        await commandor.TodoService.GetByIdAsync(2);
        await commandor.TodoService.GetByIdAsync(1);

        Assert.Equal(2, TodoService.GetByIdCalls);
    }

    [Fact]
    public async Task Command_handler_calling_Invalidate_clears_query_cache()
    {
        var commandor = BuildAppCommandor();

        await commandor.TodoService.GetByIdAsync(5);
        Assert.Equal(1, TodoService.GetByIdCalls);

        await commandor.SendAsync(new CreateTodoCommand("new"));  // handler calls Invalidate<ITodoService>

        await commandor.TodoService.GetByIdAsync(5);
        Assert.Equal(2, TodoService.GetByIdCalls);
    }

    [Fact]
    public async Task Default_parameter_value_is_preserved()
    {
        var commandor = BuildAppCommandor();

        var a = await commandor.TodoService.ListAsync();          // filter == null
        var b = await commandor.TodoService.ListAsync();          // same default → same cache entry
        var c = await commandor.TodoService.ListAsync("done");    // different filter → different entry

        Assert.Equal(2, TodoService.ListCalls);
        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public async Task Non_decorated_method_passes_straight_through()
    {
        var commandor = BuildAppCommandor();

        await commandor.TodoService.CountUntrackedAsync();
        await commandor.TodoService.CountUntrackedAsync();
        await commandor.TodoService.CountUntrackedAsync();

        // No caching applied — service is called on every invocation.
        Assert.Equal(3, TodoService.UntrackedCalls);
    }

    [Fact]
    public async Task Manual_Invalidate_via_commandor_still_works()
    {
        var commandor = BuildAppCommandor();

        await commandor.TodoService.GetByIdAsync(3);
        Assert.Equal(1, TodoService.GetByIdCalls);

        await commandor.InvalidateAsync<ITodoService>();

        await commandor.TodoService.GetByIdAsync(3);
        Assert.Equal(2, TodoService.GetByIdCalls);
    }
}
