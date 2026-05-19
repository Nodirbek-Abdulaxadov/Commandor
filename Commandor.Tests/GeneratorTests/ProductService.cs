namespace Commandor.Tests.GeneratorTests;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public record CreateProductCommand(string Name, decimal Price, int Stock) : IRequest<ProductDto>;
public record UpdateProductPriceCommand(int ProductId, decimal NewPrice) : IRequest;

/// <summary>Query-only product service (v3 shape).</summary>
public interface IProductService : ICommandorService
{
    [QueryHandler]
    Task<ProductDto?> GetProductById(int productId, CancellationToken cancellationToken = default);

    [QueryHandler]
    Task<List<ProductDto>> GetAllProducts(CancellationToken cancellationToken = default);

    [QueryHandler]
    Task<int> GetProductsCount();
}

public class ProductService : IProductService
{
    public static readonly List<ProductDto> Products = new();
    private static int _getProductByIdCallCount;
    private static int _getAllProductsCallCount;
    private static int _getProductsCountCallCount;

    public static int GetProductByIdCallCount => _getProductByIdCallCount;
    public static int GetAllProductsCallCount => _getAllProductsCallCount;
    public static int GetProductsCountCallCount => _getProductsCountCallCount;

    public static void ResetState()
    {
        Products.Clear();
        Interlocked.Exchange(ref _getProductByIdCallCount, 0);
        Interlocked.Exchange(ref _getAllProductsCallCount, 0);
        Interlocked.Exchange(ref _getProductsCountCallCount, 0);
    }

    public virtual Task<ProductDto?> GetProductById(int productId, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _getProductByIdCallCount);
        return Task.FromResult(Products.FirstOrDefault(p => p.Id == productId));
    }

    public virtual Task<List<ProductDto>> GetAllProducts(CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _getAllProductsCallCount);
        return Task.FromResult(Products.ToList());
    }

    public virtual Task<int> GetProductsCount()
    {
        Interlocked.Increment(ref _getProductsCountCallCount);
        return Task.FromResult(Products.Count);
    }
}

/// <summary>Hand-written command handler.</summary>
public sealed class CreateProductHandler(ICommandor commandor) : IRequestHandler<CreateProductCommand, ProductDto>
{
    public Task<ProductDto> HandleAsync(CreateProductCommand command, CancellationToken ct = default)
    {
        var product = new ProductDto
        {
            Id = Random.Shared.Next(1000, 9999),
            Name = command.Name,
            Price = command.Price,
            Stock = command.Stock,
        };
        ProductService.Products.Add(product);
        commandor.Invalidate<IProductService>();
        return Task.FromResult(product);
    }
}

public sealed class UpdateProductPriceHandler(ICommandor commandor) : IRequestHandler<UpdateProductPriceCommand>
{
    public Task HandleAsync(UpdateProductPriceCommand command, CancellationToken ct = default)
    {
        var product = ProductService.Products.FirstOrDefault(p => p.Id == command.ProductId);
        if (product is not null) product.Price = command.NewPrice;
        commandor.Invalidate<IProductService>();
        return Task.CompletedTask;
    }
}
