using Commandor;

namespace WebApplication1.Features;

public record CreateColorCommand(string Name, string HexCode) : IRequest<ColorEntity>;
public record UpdateColorCommand(ColorEntity Entity) : IRequest;
// Queries (Read operations - auto caching)
public record GetAllColorsQuery() : IRequest<List<ColorEntity>>;
public record GetColorQuery(long Id) : IRequest<ColorEntity?>;