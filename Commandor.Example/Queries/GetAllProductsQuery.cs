using Commandor.Example.Commands;

namespace Commandor.Example.Queries;

public record GetAllProductsQuery : IRequest<List<Product>>;
