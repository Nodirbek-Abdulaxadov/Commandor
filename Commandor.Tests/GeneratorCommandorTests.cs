using Microsoft.Extensions.DependencyInjection;
using Commandor.Tests.GeneratorTests;
using Commandor;

namespace Commandor.Tests;

public class GeneratorCommandorTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICommandor _commandor;

    public GeneratorCommandorTests()
    {
        ProductService.ResetState();
        var services = new ServiceCollection();

        // Commandorni qo'shish
        services.AddCommandor();

        // Service va uning auto-generated handlerlarini qo'shish
        services.AddCommandorService<IProductService, ProductService>();

        _serviceProvider = services.BuildServiceProvider();
        _commandor = _serviceProvider.GetRequiredService<ICommandor>();
    }

    [Fact]
    public async Task SendAsync_CreateProductCommand_ShouldWork()
    {
        // Arrange
        var command = new CreateProductCommand("iPhone 15", 15000000, 10);

        // Act
        var result = await _commandor.SendAsync(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("iPhone 15", result.Name);
        Assert.Equal(15000000, result.Price);
        Assert.Equal(10, result.Stock);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task SendAsync_UpdatePriceCommand_ShouldWork()
    {
        // Arrange
        var command = new UpdateProductPriceCommand(123, 14500000);

        // Act & Assert - Xatolik bo'lmasligi kerak
        await _commandor.SendAsync(command);
    }

    [Fact]
    public async Task SendAsync_MultipleCommands_ShouldWork()
    {
        // Arrange
        var createCommand = new CreateProductCommand("Samsung Galaxy", 12000000, 20);
        var updateCommand = new UpdateProductPriceCommand(456, 11500000);

        // Act
        var product = await _commandor.SendAsync(createCommand);
        await _commandor.SendAsync(updateCommand);

        // Assert
        Assert.NotNull(product);
        Assert.Equal("Samsung Galaxy", product.Name);
    }
    
    [Fact]
    public async Task SendAsync_QueryHandler_ShouldAutoGenerateHandler()
    {
        var createCmd = new CreateProductCommand("Test Product", 1000, 5);
        var created = await _commandor.SendAsync(createCmd);

        var product = await _commandor.SendAsync(new GetProductByIdQuery(created.Id));

        Assert.NotNull(product);
        Assert.Equal("Test Product", product.Name);
        Assert.Equal(created.Id, product.Id);
    }

    [Fact]
    public async Task SendAsync_QueryHandler_ShouldSupportCaching()
    {
        var createCmd = new CreateProductCommand("Cached Product", 2000, 10);
        var created = await _commandor.SendAsync(createCmd);
        var query = new GetProductByIdQuery(created.Id);

        var product1 = await _commandor.SendAsync(query);
        var product2 = await _commandor.SendAsync(query); // from cache

        Assert.NotNull(product1);
        Assert.NotNull(product2);
        Assert.Equal(product1.Id, product2.Id);
        Assert.Equal(1, ProductService.GetProductByIdCallCount);
    }

    // ── GetAsync (semantic alias for queries) ─────────────────────────────────

    [Fact]
    public async Task GetAsync_QueryHandler_ShouldWork()
    {
        var created = await _commandor.SendAsync(new CreateProductCommand("GetAsync Product", 3000, 5));

        // GetAsync is the preferred API for queries
        var product = await _commandor.GetAsync(new GetProductByIdQuery(created.Id));

        Assert.NotNull(product);
        Assert.Equal("GetAsync Product", product.Name);
    }

    [Fact]
    public async Task GetAsync_ShouldShareCacheWithSendAsync()
    {
        var created = await _commandor.SendAsync(new CreateProductCommand("Shared Cache", 500, 2));
        var query = new GetProductByIdQuery(created.Id);

        // Prime cache via SendAsync
        var r1 = await _commandor.SendAsync(query);
        // GetAsync must hit same cache entry
        var r2 = await _commandor.GetAsync(query);

        Assert.Equal(r1!.Id, r2!.Id);
        Assert.Equal(1, ProductService.GetProductByIdCallCount);
    }

    // ── Auto-generated extension methods ─────────────────────────────────────
    // The source generator emits a typed extension method on ICommandor per
    // [QueryHandler] method, so callers never need to type the request record.

    [Fact]
    public async Task ServiceProxy_GetProductById_ShouldWork()
    {
        var created = await _commandor.SendAsync(new CreateProductCommand("Ext Product", 9999, 1));

        // Generated proxy: commandor.ProductService.GetProductById(query) → commandor.GetAsync(query)
        var product = await _commandor.ProductService.GetProductById(new GetProductByIdQuery(created.Id));

        Assert.NotNull(product);
        Assert.Equal("Ext Product", product.Name);
    }

    [Fact]
    public async Task ServiceProxy_ShouldShareCacheWithGetAsync()
    {
        var created = await _commandor.SendAsync(new CreateProductCommand("Cache Ext", 100, 1));
        var query = new GetProductByIdQuery(created.Id);

        var r1 = await _commandor.GetAsync(query);
        var r2 = await _commandor.ProductService.GetProductById(query); // same cache key

        Assert.Equal(r1!.Id, r2!.Id);
        Assert.Equal(1, ProductService.GetProductByIdCallCount);
    }

    [Fact]
    public async Task ServiceProxy_ParameterlessQuery_WithCancellationTokenOnly_ShouldWorkAndCache()
    {
        await _commandor.SendAsync(new CreateProductCommand("P1", 10, 1));
        await _commandor.SendAsync(new CreateProductCommand("P2", 20, 2));

        var list1 = await _commandor.ProductService.GetAllProducts();
        var list2 = await _commandor.ProductService.GetAllProducts();

        Assert.Equal(2, list1.Count);
        Assert.Equal(2, list2.Count);
        Assert.Equal(1, ProductService.GetAllProductsCallCount);
    }

    [Fact]
    public async Task ServiceProxy_ParameterlessQuery_WithNoParameters_ShouldWorkAndCache()
    {
        await _commandor.SendAsync(new CreateProductCommand("P3", 30, 3));

        var count1 = await _commandor.ProductService.GetProductsCount();
        var count2 = await _commandor.ProductService.GetProductsCount();

        Assert.Equal(count1, count2);
        Assert.True(count1 >= 1);
        Assert.Equal(1, ProductService.GetProductsCountCallCount);
    }
}
