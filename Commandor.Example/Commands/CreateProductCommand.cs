namespace Commandor.Example.Commands;

public record CreateProductCommand(string Name, decimal Price) : IRequest<Product>;

public record Product
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
}
