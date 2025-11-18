using Microsoft.Extensions.DependencyInjection;
using Commandor.Tests.GeneratorTests;

namespace Commandor.Tests;

public class GeneratorCommandorTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICommandor _commandor;

    public GeneratorCommandorTests()
    {
        var services = new ServiceCollection();

        // Commandorni qo'shish
        services.AddSingleton<ICommandor, Commandor>();

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
        // Arrange - ensure we have a product first
        var createCmd = new CreateProductCommand("Test Product", 1000, 5);
        var created = await _commandor.SendAsync(createCmd);
        
        var query = new GetProductByIdQuery(created.Id);

        // Act
        var product = await _commandor.SendAsync(query);

        // Assert
        Assert.NotNull(product);
        Assert.Equal("Test Product", product.Name);
        Assert.Equal(created.Id, product.Id);
    }
    
    [Fact]
    public async Task SendAsync_QueryHandler_ShouldSupportCaching()
    {
        // Arrange
        var createCmd = new CreateProductCommand("Cached Product", 2000, 10);
        var created = await _commandor.SendAsync(createCmd);
        
        var query = new GetProductByIdQuery(created.Id);

        // Act - First call
        var product1 = await _commandor.SendAsync(query);
        
        // Act - Second call (should be same instance or at least same data)
        var product2 = await _commandor.SendAsync(query);

        // Assert
        Assert.NotNull(product1);
        Assert.NotNull(product2);
        Assert.Equal(product1.Id, product2.Id);
        Assert.Equal(product1.Name, product2.Name);
        Assert.Equal(product1.Price, product2.Price);
    }
}
