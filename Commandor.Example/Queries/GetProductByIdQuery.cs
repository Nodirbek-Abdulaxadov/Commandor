using Commandor.Example.Commands;

namespace Commandor.Example.Queries;

public record GetProductByIdQuery(int ProductId) : IRequest<Product?>;
