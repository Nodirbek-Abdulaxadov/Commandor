namespace Commandor.Example.Commands;

public record UpdateProductPriceCommand(int ProductId, decimal NewPrice) : IRequest<bool>;
