using Commandor;

namespace WebApplication1.Features;

public interface IColorService : ICommandorService
{
    [QueryHandler]
    Task<List<ColorEntity>> GetAll(GetAllColorsQuery query, CancellationToken cancellationToken = default);
    [QueryHandler]
    Task<ColorEntity?> GetById(GetColorQuery query, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task<ColorEntity> Create(CreateColorCommand command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task Update(UpdateColorCommand command, CancellationToken cancellationToken = default);
}