using Commandor.Example.Commands;
using Commandor.Example.Queries;

namespace Commandor.Example.Services;

/// <summary>
/// Product service implementation - faqat business logic
/// Handlerlar Commandor.Generators tomonidan avtomatik yaratiladi!
/// </summary>
public class ProductService : IProductService
{
    private readonly List<Product> _products = new();
    private int _nextId = 1;

    public Task<Product> CreateProduct(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Id = _nextId++,
            Name = command.Name,
            Price = command.Price
        };

        _products.Add(product);
        Console.WriteLine($"✅ Product yaratildi: {product.Name} - {product.Price:C}");

        return Task.FromResult(product);
    }

    public Task<bool> UpdateProductPrice(UpdateProductPriceCommand command, CancellationToken cancellationToken = default)
    {
        var product = _products.FirstOrDefault(p => p.Id == command.ProductId);
        if (product == null)
        {
            Console.WriteLine($"❌ Product topilmadi: ID={command.ProductId}");
            return Task.FromResult(false);
        }

        var index = _products.IndexOf(product);
        _products[index] = product with { Price = command.NewPrice };

        Console.WriteLine($"✅ Narx yangilandi: {product.Name} - {command.NewPrice:C}");
        return Task.FromResult(true);
    }

    public Task<Product?> GetProductById(GetProductByIdQuery query, CancellationToken cancellationToken = default)
    {
        var product = _products.FirstOrDefault(p => p.Id == query.ProductId);
        
        if (product != null)
            Console.WriteLine($"📦 Product topildi: {product.Name} - {product.Price:C}");
        else
            Console.WriteLine($"❌ Product topilmadi: ID={query.ProductId}");

        return Task.FromResult(product);
    }

    public Task<List<Product>> GetAllProducts(GetAllProductsQuery query, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"📋 Jami {_products.Count} ta product mavjud");
        return Task.FromResult(_products.ToList());
    }
}
