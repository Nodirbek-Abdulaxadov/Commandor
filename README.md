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

## ?? Muhim Eslatmalar

### Command/Query Pattern

Commandor'da **barcha method parametrlari `IRequest<TResponse>` yoki `IRequest` implement qilishi kerak**. Primitive type'larni to'g'ridan-to'g'ri ishlatish mumkin emas.

**? Noto'g'ri:**
```csharp
public interface ITodoService : ICommandorService
{
    [CommandHandler]
    Task<Todo> CreateTodoAsync(string title);  // ? string - IRequest emas
    
    [QueryHandler]
    Task<Todo?> GetTodoByIdAsync(int id);  // ? int - IRequest emas
}
```

**? To'g'ri:**
```csharp
// Request/Query record'larni yaratish
public record CreateTodoCommand(string Title) : IRequest<Todo>;
public record GetTodoByIdQuery(int Id) : IRequest<Todo?>;

public interface ITodoService : ICommandorService
{
    [CommandHandler]
    Task<Todo> CreateTodoAsync(CreateTodoCommand command, CancellationToken ct = default);
    
    [QueryHandler]
    Task<Todo?> GetTodoByIdAsync(GetTodoByIdQuery query, CancellationToken ct = default);
}
```

### Method Naming

Method nomlari uchun quyidagi konvensiyalarni tavsiya qilamiz:
- **Command'lar** uchun: `Create...`, `Update...`, `Delete...`, `Process...`
- **Query'lar** uchun: `Get...`, `Find...`, `List...`, `Search...`

### Cache Invalidation

**CommandHandler'lar avtomatik cache'ni tozalamaydi!** Agar command'dan keyin query cache'ini invalidate qilish kerak bo'lsa, service implementation'ida qo'lda qiling:

```csharp
public class TodoService(AppDbContext db, IComputedCache cache) : ITodoService
{
    public async Task<Todo> UpdateTodoAsync(UpdateTodoCommand cmd, CancellationToken ct)
    {
        var todo = await db.Todos.FindAsync([cmd.Id], ct);
        if (todo != null)
        {
            todo.Title = cmd.Title;
            await db.SaveChangesAsync(ct);
            
            // Cache'ni qo'lda invalidate qilish
            var cacheKey = CacheKeyBuilder.Build(
                typeof(ITodoService),
                nameof(GetTodoByIdAsync),
                new GetTodoByIdQuery(cmd.Id));
            cache.Remove(cacheKey);
        }
        return todo;
    }
}
