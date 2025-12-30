using Microsoft.Extensions.DependencyInjection;
using Commandor;
using Commandor;

namespace Commandor.Tests;

/// <summary>
/// Integration tests - hamma komponentlar birgalikda test qilinadi.
/// </summary>
public class IntegrationTests
{
    [Fact]
    public async Task FullWorkflow_CreateQueryUpdateQuery_ShouldWorkEndToEnd()
    {
        // Arrange - Setup complete system
        var services = new ServiceCollection();
        services.AddCommandor(typeof(IntegrationTests).Assembly);
        services.AddCommandorService<IntegrationTests.ITestService, IntegrationTests.TestService>();

        var serviceProvider = services.BuildServiceProvider();
        var commandor = serviceProvider.GetRequiredService<ICommandor>();

        // Act & Assert - Complete workflow
        
        // 1. Create item
        var createCmd = new CreateItemCommand("Test Item", 100);
        var created = await commandor.SendAsync(createCmd);
        Assert.NotNull(created);
        Assert.Equal("Test Item", created.Name);
        Assert.True(created.Id > 0);

        // 2. Query item (first time - from source)
        var query1 = new GetItemQuery(created.Id);
        var item1 = await commandor.SendAsync(query1);
        Assert.NotNull(item1);
        Assert.Equal(created.Id, item1.Id);

        // 3. Query again (should be cached)
        var query2 = new GetItemQuery(created.Id);
        var item2 = await commandor.SendAsync(query2);
        Assert.NotNull(item2);
        Assert.Equal(item1.Id, item2.Id);

        // 4. Update item
        var updateCmd = new UpdateItemCommand(created.Id, "Updated Item", 200);
        await commandor.SendAsync(updateCmd);
        
        // 5. Query after update (cache invalidated, fresh data)
        var query3 = new GetItemQuery(created.Id);
        var item3 = await commandor.SendAsync(query3);
        Assert.NotNull(item3);
        Assert.Equal("Updated Item", item3.Name);
        Assert.Equal(200, item3.Value);
    }

    [Fact]
    public void AllComponents_ShouldBeAvailable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCommandor(typeof(IntegrationTests).Assembly);
        services.AddCommandorService<ITestService, TestService>();

        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert - All components should be resolvable
        var commandor = serviceProvider.GetService<ICommandor>();
        var service = serviceProvider.GetService<ITestService>();

        Assert.NotNull(commandor);
        Assert.NotNull(service);
    }

    // Test models
    public record CreateItemCommand(string Name, int Value) : IRequest<ItemDto>;
    public record UpdateItemCommand(int Id, string Name, int Value) : IRequest;
    public record GetItemQuery(int Id) : IRequest<ItemDto?>;

    public class ItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public interface ITestService : ICommandorService
    {
        [CommandHandler]
        Task<ItemDto> CreateItem(CreateItemCommand cmd, CancellationToken ct = default);

        [CommandHandler]
        Task UpdateItem(UpdateItemCommand cmd, CancellationToken ct = default);

        [QueryHandler]
        Task<ItemDto?> GetItem(GetItemQuery query, CancellationToken ct = default);
    }

    public class TestService : ITestService
    {
        private static readonly List<ItemDto> _items = new();
        private readonly ICommandor _commandor;

        public TestService(ICommandor commandor)
        {
            _commandor = commandor;
        }

        public Task<ItemDto> CreateItem(CreateItemCommand cmd, CancellationToken ct = default)
        {
            var item = new ItemDto
            {
                Id = Random.Shared.Next(1000, 9999),
                Name = cmd.Name,
                Value = cmd.Value
            };
            _items.Add(item);
            return Task.FromResult(item);
        }

        public Task UpdateItem(UpdateItemCommand cmd, CancellationToken ct = default)
        {
            var item = _items.FirstOrDefault(i => i.Id == cmd.Id);
            if (item != null)
            {
                item.Name = cmd.Name;
                item.Value = cmd.Value;
            }
            
            // Invalidate cache
            _commandor.Invalidate<ITestService>();
            
            return Task.CompletedTask;
        }

        public Task<ItemDto?> GetItem(GetItemQuery query, CancellationToken ct = default)
        {
            var item = _items.FirstOrDefault(i => i.Id == query.Id);
            return Task.FromResult(item);
        }
    }
}
