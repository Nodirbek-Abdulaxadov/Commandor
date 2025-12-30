# Commandor

A lightweight CQRS/Mediator library for .NET with automatic caching and source generation.

## Installation

```bash
dotnet add package ICommandor
```

## Quick Start

### 1. Define Your Service Interface

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
```

### 2. Implement Your Service

```csharp
public class ColorService : IColorService
{
    private readonly AppDbContext _db;
    private readonly ICommandor _commandor;

    public ColorService(AppDbContext db, ICommandor commandor)
    {
        _db = db;
        _commandor = commandor;
    }

    public async Task<ColorEntity> Create(CreateColorCommand command, CancellationToken ct = default)
    {
        var color = new ColorEntity { Name = command.Name };
        _db.Colors.Add(color);
        await _db.SaveChangesAsync(ct);
        
        // Invalidate cache after mutation
        _commandor.Invalidate<IColorService>();
        
        return color;
    }

    public Task<List<ColorEntity>> GetAll(GetAllColorsQuery query, CancellationToken ct = default)
        => _db.Colors.ToListAsync(ct);

    public Task<ColorEntity?> GetById(GetColorQuery query, CancellationToken ct = default)
        => _db.Colors.FindAsync(new object[] { query.Id }, ct).AsTask();

    public async Task Update(UpdateColorCommand command, CancellationToken ct = default)
    {
        var color = await _db.Colors.FindAsync(new object[] { command.Id }, ct);
        if (color != null)
        {
            color.Name = command.Name;
            await _db.SaveChangesAsync(ct);
            
            // Invalidate cache after mutation
            _commandor.Invalidate<IColorService>();
        }
    }
}
```

### 3. Register Services

```csharp
var services = new ServiceCollection();
services.AddCommandor();
services.AddCommandorService<IColorService, ColorService>();
```

### 4. Use in Controllers

```csharp
public class ColorsController : ControllerBase
{
    private readonly ICommandor _commandor;

    public ColorsController(ICommandor commandor) => _commandor = commandor;

    [HttpGet]
    public Task<List<ColorEntity>> GetAll()
        => _commandor.SendAsync(new GetAllColorsQuery());

    [HttpPost]
    public Task<ColorEntity> Create(CreateColorCommand command)
        => _commandor.SendAsync(command);
}
```

## Features

### Automatic Caching

- `[QueryHandler]` results are automatically cached using `IMemoryCache`
- Cache is invalidated per service using `ICommandor.Invalidate<TService>()`
- No manual cache key management needed

### Cache Invalidation

**New in v1.0.7:** Use `ICommandor` for cache invalidation:

```csharp
// Invalidate all cached queries for a service
_commandor.Invalidate<ITodoService>();

// Or async version
await _commandor.InvalidateAsync<ITodoService>();
```

### Source Generation

Commandor automatically generates handler classes for methods marked with `[CommandHandler]` or `[QueryHandler]`. No boilerplate code needed!

## Important Notes

### Command/Query Pattern

**All method parameters must implement `IRequest<TResponse>` or `IRequest`.** Primitive types cannot be used directly.

**❌ Incorrect:**
```csharp
public interface ITodoService : ICommandorService
{
    [CommandHandler]
    Task<Todo> CreateTodoAsync(string title);  // ❌ string is not IRequest
    
    [QueryHandler]
    Task<Todo?> GetTodoByIdAsync(int id);  // ❌ int is not IRequest
}
```

**✅ Correct:**
```csharp
// Define request/query records
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

### Method Naming Conventions

Recommended naming patterns:
- **Commands**: `Create...`, `Update...`, `Delete...`, `Process...`
- **Queries**: `Get...`, `Find...`, `List...`, `Search...`

### Cache TTL (Optional)

Specify cache duration per query:

```csharp
[QueryHandler(CacheTtlSeconds = 300)]  // Cache for 5 minutes
Task<List<Product>> GetProducts(GetProductsQuery query, CancellationToken ct = default);
```

## Migration from v1.0.6

If upgrading from v1.0.6 or earlier:

**Old:**
```csharp
cache.Remove(cacheKey);  // Manual cache key management
```

**New:**
```csharp
_commandor.Invalidate<IYourService>();  // Service-level invalidation
```

## License

MIT
