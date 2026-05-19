using Commandor;
using Commandor.Generated;
using Commandor.Tests.GeneratorTests;
using Microsoft.Extensions.DependencyInjection;

namespace Commandor.Tests;

/// <summary>
/// End-to-end against <see cref="IProductService"/>: writes happen via
/// hand-written command handlers + <c>SendAsync</c>; reads happen via
/// the generated <see cref="AppCommandor.ProductService"/> property.
/// </summary>
public class GeneratorCommandorTests
{
    private readonly AppCommandor _commandor;

    public GeneratorCommandorTests()
    {
        ProductService.ResetState();
        var services = new ServiceCollection();
        services.AddCommandor(typeof(GeneratorCommandorTests).Assembly);
        services.AddCommandorService<IProductService, ProductService>();
        services.AddAppCommandor();
        _commandor = services.BuildServiceProvider().GetRequiredService<AppCommandor>();
    }

    [Fact]
    public async Task CreateProductCommand_returns_product()
    {
        var result = await _commandor.SendAsync(new CreateProductCommand("iPhone 15", 15000000, 10));

        Assert.NotNull(result);
        Assert.Equal("iPhone 15", result.Name);
        Assert.Equal(15000000, result.Price);
        Assert.Equal(10, result.Stock);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task UpdatePriceCommand_runs_without_error()
    {
        await _commandor.SendAsync(new UpdateProductPriceCommand(123, 14500000));
    }

    [Fact]
    public async Task Query_via_service_property_caches()
    {
        var created = await _commandor.SendAsync(new CreateProductCommand("Cached", 2000, 10));

        var p1 = await _commandor.ProductService.GetProductById(created.Id);
        var p2 = await _commandor.ProductService.GetProductById(created.Id); // cache hit

        Assert.NotNull(p1);
        Assert.NotNull(p2);
        Assert.Equal(p1!.Id, p2!.Id);
        Assert.Equal(1, ProductService.GetProductByIdCallCount);
    }

    [Fact]
    public async Task Update_invalidates_cached_query()
    {
        var created = await _commandor.SendAsync(new CreateProductCommand("Refresh", 100, 1));

        var p1 = await _commandor.ProductService.GetProductById(created.Id);
        Assert.Equal(1, ProductService.GetProductByIdCallCount);

        await _commandor.SendAsync(new UpdateProductPriceCommand(created.Id, 999));

        var p2 = await _commandor.ProductService.GetProductById(created.Id); // cache cleared
        Assert.NotNull(p2);
        Assert.Equal(999, p2!.Price);
        Assert.Equal(2, ProductService.GetProductByIdCallCount);
    }

    [Fact]
    public async Task Parameterless_list_query_caches()
    {
        await _commandor.SendAsync(new CreateProductCommand("P1", 10, 1));
        await _commandor.SendAsync(new CreateProductCommand("P2", 20, 2));

        // Reset call counts after the SendAsync invalidations above so we can
        // observe just the read pattern below.
        ProductService.ResetState();
        await _commandor.SendAsync(new CreateProductCommand("Setup", 1, 1));
        ProductService.ResetState();
        // re-seed without invalidating after this:
        ProductService.Products.Add(new ProductDto { Id = 1, Name = "x", Price = 1, Stock = 1 });

        var list1 = await _commandor.ProductService.GetAllProducts();
        var list2 = await _commandor.ProductService.GetAllProducts();

        Assert.Equal(list1.Count, list2.Count);
        Assert.Equal(1, ProductService.GetAllProductsCallCount);
    }

    [Fact]
    public async Task Parameterless_count_query_caches()
    {
        ProductService.Products.Add(new ProductDto { Id = 99, Name = "n", Price = 1, Stock = 1 });

        var count1 = await _commandor.ProductService.GetProductsCount();
        var count2 = await _commandor.ProductService.GetProductsCount();

        Assert.Equal(count1, count2);
        Assert.True(count1 >= 1);
        Assert.Equal(1, ProductService.GetProductsCountCallCount);
    }
}
