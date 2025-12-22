using Commandor;

namespace WebApplication1.Features;

public class ColorService : IColorService
{
    public virtual Task<ColorEntity> Create(CreateColorCommand command, CancellationToken cancellationToken = default)
    {
        //men shu yerda chaqirishim kerak shunchaki
        this.InvalidateServiceCache();

        var color = new ColorEntity
        {
            Id = ColorDB.Colors.Count + 1,
            Name = command.Name,
            HexCode = command.HexCode
        };
        ColorDB.Colors.Add(color);
        return Task.FromResult(color);
    }

    public virtual Task<List<ColorEntity>> GetAll(GetAllColorsQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ColorDB.Colors);
    }

    public Task<ColorEntity?> GetById(GetColorQuery query, CancellationToken cancellationToken = default)
    {
        var color = ColorDB.Colors.FirstOrDefault(c => c.Id == query.Id);
        return Task.FromResult(color);
    }

    public Task Update(UpdateColorCommand command, CancellationToken cancellationToken = default)
    {
        var color = ColorDB.Colors.FirstOrDefault(c => c.Id == command.Entity.Id);
        if (color != null)
        {
            color.Name = command.Entity.Name;
            color.HexCode = command.Entity.HexCode;
        }
        return Task.CompletedTask;
    }
}


public static class ColorDB
{
    public static List<ColorEntity> Colors { get; } = new();
}