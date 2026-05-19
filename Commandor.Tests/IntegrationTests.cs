using Commandor;
using Commandor.Generated;
using Microsoft.Extensions.DependencyInjection;

namespace Commandor.Tests;

/// <summary>
/// End-to-end CQRS flow against the v3 surface:
///   - Queries flow through <c>AppCommandor.ItemService</c> (cached).
///   - Commands flow through <c>SendAsync</c> with hand-written
///     <see cref="IRequestHandler{TRequest, TResponse}"/> implementations.
///   - The command handlers call <c>Invalidate</c> when their writes
///     should drop cached query results.
/// </summary>
public class IntegrationTests
{
    [Fact]
    public async Task Create_then_query_returns_cached_then_invalidates_on_update()
    {
        var services = new ServiceCollection();
        services.AddCommandor(typeof(IntegrationTests).Assembly);
        services.AddCommandorService<ITestService, TestService>();
        services.AddAppCommandor();
        TestService.Reset();

        var sp = services.BuildServiceProvider();
        var commandor = sp.GetRequiredService<AppCommandor>();

        // Command: create.
        var created = await commandor.SendAsync(new CreateItemCommand("Test", 100));
        Assert.Equal("Test", created.Name);

        // Query: pull twice — second should be cached.
        var first = await commandor.TestService.GetItem(created.Id);
        var second = await commandor.TestService.GetItem(created.Id);
        Assert.Equal(created.Id, first!.Id);
        Assert.Equal(1, TestService.GetItemCalls);

        // Command: update — handler invalidates ITestService cache.
        await commandor.SendAsync(new UpdateItemCommand(created.Id, "Updated", 200));

        // Query: fresh hit since cache was cleared.
        var third = await commandor.TestService.GetItem(created.Id);
        Assert.Equal("Updated", third!.Name);
        Assert.Equal(2, TestService.GetItemCalls);
    }

    [Fact]
    public void All_components_resolvable()
    {
        var services = new ServiceCollection();
        services.AddCommandor(typeof(IntegrationTests).Assembly);
        services.AddCommandorService<ITestService, TestService>();
        services.AddAppCommandor();

        var sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetService<ICommandor>());
        Assert.NotNull(sp.GetService<AppCommandor>());
        Assert.NotNull(sp.GetService<ITestService>());
    }

    // ── Domain ────────────────────────────────────────────────────────────────

    public record CreateItemCommand(string Name, int Value) : IRequest<ItemDto>;
    public record UpdateItemCommand(int Id, string Name, int Value) : IRequest;

    public class ItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    /// <summary>Query-only contract. Commands moved to dedicated handlers.</summary>
    public interface ITestService : ICommandorService
    {
        [QueryHandler]
        Task<ItemDto?> GetItem(int id, CancellationToken ct = default);
    }

    public class TestService : ITestService
    {
        public static readonly List<ItemDto> Items = new();
        public static int GetItemCalls;

        public static void Reset()
        {
            Items.Clear();
            GetItemCalls = 0;
        }

        public Task<ItemDto?> GetItem(int id, CancellationToken ct = default)
        {
            Interlocked.Increment(ref GetItemCalls);
            return Task.FromResult(Items.FirstOrDefault(i => i.Id == id));
        }
    }

    public sealed class CreateItemHandler(ICommandor commandor) : IRequestHandler<CreateItemCommand, ItemDto>
    {
        public Task<ItemDto> HandleAsync(CreateItemCommand cmd, CancellationToken ct = default)
        {
            var item = new ItemDto
            {
                Id = Random.Shared.Next(1000, 9999),
                Name = cmd.Name,
                Value = cmd.Value,
            };
            TestService.Items.Add(item);
            commandor.Invalidate<ITestService>();
            return Task.FromResult(item);
        }
    }

    public sealed class UpdateItemHandler(ICommandor commandor) : IRequestHandler<UpdateItemCommand>
    {
        public Task HandleAsync(UpdateItemCommand cmd, CancellationToken ct = default)
        {
            var item = TestService.Items.FirstOrDefault(i => i.Id == cmd.Id);
            if (item is not null)
            {
                item.Name = cmd.Name;
                item.Value = cmd.Value;
            }
            commandor.Invalidate<ITestService>();
            return Task.CompletedTask;
        }
    }
}
