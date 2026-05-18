# Commandor

[![NuGet](https://img.shields.io/nuget/v/ICommandor.svg)](https://www.nuget.org/packages/ICommandor/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)

A lightweight CQRS/Mediator library for .NET with **transparent caching and source-generated proxies**.
Just decorate your service interface, inject the service directly, and queries are memoized automatically — no mediator boilerplate at the call site.

## Install

```bash
dotnet add package ICommandor
```

## 60-second tour

```csharp
// 1.  Decorate the service interface.
public interface ITodoService : ICommandorService
{
    [QueryHandler(CacheTtlSeconds = 60)]
    Task<TodoItem?> GetByIdAsync(int id, CancellationToken ct = default);

    [QueryHandler]
    Task<List<TodoItem>> ListAsync(string? filter = null, CancellationToken ct = default);

    [CommandHandler]
    Task<TodoItem> CreateAsync(string title, CancellationToken ct = default);
}

// 2.  Write the real implementation — no caching code, no invalidation calls.
public class TodoService(AppDbContext db) : ITodoService
{
    public Task<TodoItem?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Todos.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<List<TodoItem>> ListAsync(string? filter = null, CancellationToken ct = default) =>
        db.Todos.Where(t => filter == null || t.Title.Contains(filter)).ToListAsync(ct);

    public async Task<TodoItem> CreateAsync(string title, CancellationToken ct = default)
    {
        var t = new TodoItem { Title = title };
        db.Todos.Add(t);
        await db.SaveChangesAsync(ct);
        return t;
    }
}

// 3.  Register.
builder.Services.AddCommandor();
builder.Services.AddCommandorService<ITodoService, TodoService>();

// 4.  Use it — directly. No mediator, no records.
public class TodosController(ITodoService todos) : ControllerBase
{
    [HttpGet("{id}")]
    public Task<TodoItem?> Get(int id) => todos.GetByIdAsync(id);   // ← cached
    [HttpGet]            public Task<List<TodoItem>> List(string? q) => todos.ListAsync(q);
    [HttpPost]           public Task<TodoItem>       Add([FromBody] CreateBody b)
                              => todos.CreateAsync(b.Title);          // ← auto-invalidates the cache
}
```

That's it. Subsequent calls to `GetByIdAsync(7)` return the cached result. The first `CreateAsync(...)` after that wipes the service's cache automatically.

## How it works

For every interface decorated with `[QueryHandler]` / `[CommandHandler]` methods, the source generator emits a sealed `*CachedProxy` class:

```csharp
[GeneratedProxy(typeof(ITodoService))]
internal sealed class TodoServiceCachedProxy : ITodoService
{
    public TodoServiceCachedProxy(ITodoService impl, IMemoryCache cache, CommandorContext context) { … }

    public async Task<TodoItem?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var cacheKey = CacheKeyBuilder.Build(typeof(ITodoService), "GetByIdAsync", id);
        if (_cache.TryGetValue<TodoItem?>(cacheKey, out var cached)) return cached;
        var result = await _impl.GetByIdAsync(id, ct).ConfigureAwait(false);
        // … store in cache, attach invalidation token …
        return result;
    }

    public async Task<TodoItem> CreateAsync(string title, CancellationToken ct = default)
    {
        var result = await _impl.CreateAsync(title, ct).ConfigureAwait(false);
        _context.Invalidate(typeof(ITodoService));
        return result;
    }
}
```

`AddCommandorService<TService, TImpl>()`:

- registers the concrete `TImpl` (so the proxy can resolve it),
- wires `TService` → the generated proxy (via `ActivatorUtilities.CreateInstance`),
- additionally registers the per-method `IRequestHandler<,>` classes used by the legacy mediator path.

## Caching details

- Storage: `IMemoryCache`.
- Cache key: `ServiceType.MethodName(arg1, arg2, …)`. Primitive and `Guid` / `DateTime{Offset}` arguments contribute their values; complex objects contribute `TypeName#GetHashCode()`. Records work out of the box because their `GetHashCode()` is value-based.
- Invalidation: `CommandorContext` keeps one `CancellationTokenSource` per service type; every cached entry depends on its token. `Invalidate(typeof(TService))` cancels the token → all entries dependent on it are dropped on next read.
- Lifetime: defaults to "until invalidated". Set `[QueryHandler(CacheTtlSeconds = N)]` to add an absolute expiry.

## Manual invalidation

Auto-invalidation covers the common case. If a single command needs to clear other services' caches, call `Invalidate` explicitly:

```csharp
public async Task<TodoItem> CreateAsync(string title, CancellationToken ct = default)
{
    var item = await base.CreateAsync(title, ct);
    await _commandor.InvalidateAsync<IDashboardService>(ct);   // separate service
    return item;
}
```

## Legacy mediator path (still supported)

The generator continues to emit per-method `IRequestHandler<,>` classes and `ICommandor` extension methods, so all of these still work:

```csharp
await commandor.SendAsync(new CreateTodoCommand("buy milk"));
var todo = await commandor.GetAsync(new GetTodoByIdQuery(7));
var todo2 = await commandor.GetTodoByIdAsync(7);   // generator-emitted extension
```

You can mix and match — they share the same cache.

## License

MIT — see [LICENSE.txt](LICENSE.txt).
