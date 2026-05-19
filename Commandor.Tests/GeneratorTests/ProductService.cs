namespace Commandor.Tests.GeneratorTests;

/// <summary>
/// Mahsulot ma'lumotlari
/// </summary>
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

/// <summary>
/// Mahsulot yaratish command
/// </summary>
public record CreateProductCommand(string Name, decimal Price, int Stock) : IRequest<ProductDto>;

/// <summary>
/// Mahsulot narxini yangilash command
/// </summary>
public record UpdateProductPriceCommand(int ProductId, decimal NewPrice) : IRequest;

/// <summary>
/// Mahsulot olish query (caching test uchun)
/// </summary>
public record GetProductByIdQuery(int ProductId) : IRequest<ProductDto?>;

/// <summary>
/// Mahsulot servisi - [CommandHandler] va [QueryHandler] attribute'lar bilan
/// </summary>
public interface IProductService : ICommandorService
{
    [CommandHandler]
    Task<ProductDto> CreateProduct(CreateProductCommand command, CancellationToken cancellationToken = default);

    [CommandHandler]
    Task UpdatePrice(UpdateProductPriceCommand command, CancellationToken cancellationToken = default);

    [QueryHandler]  // Auto-caching uchun
    Task<ProductDto?> GetProductById(GetProductByIdQuery query, CancellationToken cancellationToken = default);

    [QueryHandler]
    Task<List<ProductDto>> GetAllProducts(CancellationToken cancellationToken = default);

    [QueryHandler]
    Task<int> GetProductsCount();
}

/// <summary>
/// Product service implementatsiyasi
/// </summary>
public class ProductService : IProductService
{
    private static readonly List<ProductDto> _products = new();
    private static int _getProductByIdCallCount;
    private static int _getAllProductsCallCount;
    private static int _getProductsCountCallCount;

    public static int GetProductByIdCallCount => _getProductByIdCallCount;
    public static int GetAllProductsCallCount => _getAllProductsCallCount;
    public static int GetProductsCountCallCount => _getProductsCountCallCount;

    public static void ResetState()
    {
        _products.Clear();
        System.Threading.Interlocked.Exchange(ref _getProductByIdCallCount, 0);
        System.Threading.Interlocked.Exchange(ref _getAllProductsCallCount, 0);
        System.Threading.Interlocked.Exchange(ref _getProductsCountCallCount, 0);
    }

    public virtual Task<ProductDto> CreateProduct(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        var product = new ProductDto
        {
            Id = Random.Shared.Next(1000, 9999),
            Name = command.Name,
            Price = command.Price,
            Stock = command.Stock
        };

        _products.Add(product);
        return Task.FromResult(product);
    }

    public virtual Task UpdatePrice(UpdateProductPriceCommand command, CancellationToken cancellationToken = default)
    {
        var product = _products.FirstOrDefault(p => p.Id == command.ProductId);
        if (product != null)
        {
            product.Price = command.NewPrice;
        }
        return Task.CompletedTask;
    }

    public virtual Task<ProductDto?> GetProductById(GetProductByIdQuery query, CancellationToken cancellationToken = default)
    {
        System.Threading.Interlocked.Increment(ref _getProductByIdCallCount);
        var product = _products.FirstOrDefault(p => p.Id == query.ProductId);
        return Task.FromResult(product);
    }

    public virtual Task<List<ProductDto>> GetAllProducts(CancellationToken cancellationToken = default)
    {
        System.Threading.Interlocked.Increment(ref _getAllProductsCallCount);
        return Task.FromResult(_products.ToList());
    }

    public virtual Task<int> GetProductsCount()
    {
        System.Threading.Interlocked.Increment(ref _getProductsCountCallCount);
        return Task.FromResult(_products.Count);
    }
}
