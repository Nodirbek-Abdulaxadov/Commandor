# Commandor

## O'rnatish

```bash
dotnet add package ICommandor
```

## Ishlatish

```csharp
using Commandor;

public interface IColorService : ICommandorService
{
    [QueryHandler]
    Task<List<ColorEntity>> GetAll(GetAllColorsQuery query, CancellationToken ct = default);

    [QueryHandler]
    Task<ColorEntity?> GetById(GetColorQuery query, CancellationToken ct = default);

    [CommandHandler]
    Task<ColorEntity> Create(CreateColorCommand command, CancellationToken ct = default);

    [CommandHandler]
    Task Update(UpdateColorCommand command, CancellationToken ct = default);
}

public class ColorService : IColorService
{
    public virtual Task<ColorEntity> Create(CreateColorCommand command, CancellationToken ct = default) => throw new NotImplementedException();
    public virtual Task<List<ColorEntity>> GetAll(GetAllColorsQuery query, CancellationToken ct = default) => throw new NotImplementedException();
    public virtual Task<ColorEntity?> GetById(GetColorQuery query, CancellationToken ct = default) => throw new NotImplementedException();
    public virtual Task Update(UpdateColorCommand command, CancellationToken ct = default) => throw new NotImplementedException();
}

var services = new ServiceCollection();
services.AddCommandor();
services.AddCommandorService<IColorService, ColorService>();
```

## Cache

- `[QueryHandler]` natijalari cache'lanadi.
- `[CommandHandler]` bajarilganda cache tozalanadi (`IComputedCache.Clear()`).
