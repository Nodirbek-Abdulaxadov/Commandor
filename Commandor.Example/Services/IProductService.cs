using Commandor.Example.Commands;
using Commandor.Example.Queries;

namespace Commandor.Example.Services;

/// <summary>
/// Product service - barcha product operatsiyalari uchun
/// [CommandHandler] attribute handler avtomatik yaratadi
/// </summary>
public interface IProductService : ICommandorService
{
    [CommandHandler]
    Task<Product> CreateProduct(CreateProductCommand command, CancellationToken cancellationToken = default);

    [CommandHandler]
    Task<bool> UpdateProductPrice(UpdateProductPriceCommand command, CancellationToken cancellationToken = default);

    [QueryHandler]  // GET - keyinchalik caching qo'shiladi
    Task<Product?> GetProductById(GetProductByIdQuery query, CancellationToken cancellationToken = default);

    [QueryHandler]  // GET - keyinchalik caching qo'shiladi
    Task<List<Product>> GetAllProducts(GetAllProductsQuery query, CancellationToken cancellationToken = default);
}
