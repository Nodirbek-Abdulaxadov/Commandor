# Commandor

A lightweight CQRS/Mediator library for .NET with automatic caching and Roslyn source generation.

## Requirements

- .NET 10 SDK (or newer) on the build machine
- `<LangVersion>latest</LangVersion>` (or `14`+) in the consuming project — the source generator emits a C# 14 extension property for the service-grouped call site
- Target framework: `net8.0`, `net9.0`, or `net10.0`

## Installation

```bash
dotnet add package ICommandor
```

```xml
<PropertyGroup>
  <LangVersion>latest</LangVersion>
</PropertyGroup>
```

## Quick Start

### 1. Define Your Service Interface

Commandor supports two styles for query parameters — **plain-type** (recommended for most queries) and **IRequest-based**.

```csharp
using Commandor;

public interface ITodoService : ICommandorService
{
    // Commands always use an IRequest record
    [CommandHandler]
    Task<Todo> CreateTodoAsync(CreateTodoCommand command, CancellationToken ct = default);

    [CommandHandler]
    Task<bool> DeleteTodoAsync(DeleteTodoCommand command, CancellationToken ct = default);

    // Plain-type query — just pass the raw parameters, no wrapper record needed.
    // The generator creates the IRequest record and the proxy method for you.
    [QueryHandler]
    Task<Todo?> GetTodoByIdAsync(int id, CancellationToken ct = default);

    // IRequest-based query — optional. You can also use parameterless queries directly.
    [QueryHandler]
    Task<List<Todo>> GetAllTodosAsync(GetAllTodosQuery query, CancellationToken ct = default);
}
```

### 2. Define Request Records

Commands always need request records. IRequest-based queries need them too. Plain-type queries do **not** — the generator creates an internal wrapper automatically.

```csharp
// Commands
public record CreateTodoCommand(string Title) : IRequest<Todo>;
public record DeleteTodoCommand(int Id) : IRequest<bool>;

// Zero-param query still needs a wrapper record
public record GetAllTodosQuery() : IRequest<List<Todo>>;

// GetTodoByIdQuery is NOT needed — the generator creates it internally
// when the method uses plain-type parameters (int id).
```

### 3. Implement Your Service

```csharp
public class TodoService(AppDbContext db, ICommandor commandor) : ITodoService
{
    public async Task<Todo> CreateTodoAsync(CreateTodoCommand command, CancellationToken ct = default)
    {
        await commandor.InvalidateAsync<ITodoService>(ct); // clear stale cache
        var todo = new Todo { Title = command.Title };
        db.Todos.Add(todo);
        await db.SaveChangesAsync(ct);
        return todo;
    }

    public async Task<bool> DeleteTodoAsync(DeleteTodoCommand command, CancellationToken ct = default)
    {
        await commandor.InvalidateAsync<ITodoService>(ct);
        var todo = await db.Todos.FindAsync([command.Id], ct);
        if (todo == null) return false;
        db.Todos.Remove(todo);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // Plain-type method — signature matches the interface exactly
    public Task<Todo?> GetTodoByIdAsync(int id, CancellationToken ct = default)
        => db.Todos.FindAsync([id], ct).AsTask();

    public Task<List<Todo>> GetAllTodosAsync(GetAllTodosQuery query, CancellationToken ct = default)
        => db.Todos.ToListAsync(ct);
}
```

### 4. Register Services

```csharp
// Register Commandor core + memory cache
builder.Services.AddCommandor();

// Register your service + its auto-generated handlers
builder.Services.AddCommandorService<ITodoService, TodoService>();
```

> **Tip:** If you want to scan an assembly for manually-written `IRequestHandler<>` implementations, use the generic overload:
> ```csharp
> builder.Services.AddCommandor<Program>(); // scans the assembly that contains Program
> ```

### 5. Use in Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class TodosController(ICommandor commandor) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateTodo([FromBody] CreateTodoCommand request)
    {
        var todo = await commandor.SendAsync(request);
        return CreatedAtAction(nameof(GetTodoById), new { id = todo.Id }, todo);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTodoById(int id)
    {
        // Generated service proxy — queries are grouped under commandor.<Service>
        var todo = await commandor.TodoService.GetTodoByIdAsync(id);
        return todo is null ? NotFound() : Ok(todo);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllTodos()
    {
        // IRequest-mode query through the same proxy
        var todos = await commandor.TodoService.GetAllTodosAsync(new GetAllTodosQuery());
        return Ok(todos);
    }
}
```

---

## Features

### Service-Grouped Query Proxy

Every interface with `[QueryHandler]` methods exposes its queries through a proxy reached by an extension property on `ICommandor`. The property name is derived from the interface (`IProductService` → `commandor.ProductService`).

```csharp
public interface IProductService : ICommandorService
{
    [QueryHandler(CacheTtlSeconds = 60)]
    Task<Product?> GetProductByIdAsync(int id, CancellationToken ct = default);

    [QueryHandler]
    Task<List<Product>> GetAllProductsAsync(CancellationToken ct = default);
}

// Generated (invisible — lives in generated code):
//   internal sealed record GetProductByIdAsyncRequest(int Id) : IRequest<Product?>;
//   public readonly struct ProductServiceCommandorProxy { ... }
//   public static class CommandorProductServiceExtensions
//   {
//       extension(ICommandor commandor)
//       {
//           public ProductServiceCommandorProxy ProductService => new(commandor);
//       }
//   }

// Call site — grouped by service, no wrapper record in sight:
var product  = await commandor.ProductService.GetProductByIdAsync(42);
var products = await commandor.ProductService.GetAllProductsAsync();
```

> **Commands are not exposed through the proxy.** Dispatch commands with `commandor.SendAsync(command)`.

### Plain-Type Query Parameters

For `[QueryHandler]` methods, you can use any parameter types — no need to manually create an `IRequest<T>` record. The generator creates an internal wrapper record and a typed method on the service proxy:

```csharp
[QueryHandler(CacheTtlSeconds = 60)]
Task<Product?> GetProductByIdAsync(int id, CancellationToken ct = default);

// Call site:
var product = await commandor.ProductService.GetProductByIdAsync(productId);
```

### GetAsync — Semantic Query API

`GetAsync` is a semantic alias for `SendAsync` when dispatching queries. Use `SendAsync` for commands and `GetAsync` for queries — the behaviour is identical, but the intent is clearer.

```csharp
// All three share the same cache entry:
var result1 = await commandor.SendAsync(new GetProductByIdQuery(id));
var result2 = await commandor.GetAsync(new GetProductByIdQuery(id));
var result3 = await commandor.ProductService.GetProductByIdAsync(id); // preferred
```

### Automatic Caching

- Every `[QueryHandler]` result is cached automatically via `IMemoryCache`.
- Cache keys are built from the method name and a hashcode of the arguments — fast and allocation-light, no JSON serialisation.
- Records use value-based `GetHashCode()` so structurally-equal queries share the same cache entry.

### Cache Invalidation

```csharp
// Synchronous
commandor.Invalidate<ITodoService>();

// Async (preferred — flushes the change token before returning)
await commandor.InvalidateAsync<ITodoService>(cancellationToken);
```

### Cache TTL

```csharp
[QueryHandler(CacheTtlSeconds = 300)]  // 5-minute absolute expiry
Task<List<Product>> GetProductsAsync(GetProductsQuery query, CancellationToken ct = default);
```

### Source Generation

Commandor uses an **incremental Roslyn source generator** (`IIncrementalGenerator`). It generates handler classes, request wrappers, and service proxies for every `[CommandHandler]` and `[QueryHandler]` at compile time — no reflection at startup, no boilerplate.

Generated handler classes are decorated with `[GeneratedHandler]` so the registration helpers can discover them precisely without fragile name heuristics.

---

## Command Pattern

Commands mutate state and use `IRequest` records:

```csharp
// 1. Define the request record
public record CreateProductCommand(string Name, decimal Price) : IRequest<Product>;

// 2. Mark the interface method
public interface IProductService : ICommandorService
{
    [CommandHandler]
    Task<Product> CreateProductAsync(CreateProductCommand command, CancellationToken ct = default);
}

// 3. Dispatch
var product = await commandor.SendAsync(new CreateProductCommand("Widget", 9.99m));
```

Commands are **not** cached. Invalidate related query caches inside the implementation.

---

## Query Pattern

Queries are read-only and are automatically cached. Every `[QueryHandler]` is reachable through `commandor.<ServiceName>.<MethodName>(...)`.

### IRequest-Based (required for zero-param queries that take a query record)

```csharp
public record GetAllProductsQuery() : IRequest<List<Product>>;

public interface IProductService : ICommandorService
{
    [QueryHandler(CacheTtlSeconds = 60)]
    Task<List<Product>> GetAllProductsAsync(GetAllProductsQuery query, CancellationToken ct = default);
}

var products = await commandor.ProductService.GetAllProductsAsync(new GetAllProductsQuery());
```

### Plain-Type (recommended for parameterized queries)

```csharp
public interface IProductService : ICommandorService
{
    [QueryHandler(CacheTtlSeconds = 60)]
    Task<Product?> GetProductByIdAsync(int id, CancellationToken ct = default);
}

// No record to construct — just pass the value:
var product = await commandor.ProductService.GetProductByIdAsync(42);
```

### Truly Parameterless (no parameters at all, not even a query record)

```csharp
public interface IProductService : ICommandorService
{
    [QueryHandler]
    Task<int> GetProductsCountAsync();
}

var count = await commandor.ProductService.GetProductsCountAsync();
```

---

## Migration from 1.x → 4.0

The flat extension methods generated by 1.x are removed. Move call sites under the service proxy:

```csharp
// before (1.x)
await commandor.GetProductByIdAsync(id);
await commandor.GetAllProductsAsync(new GetAllProductsQuery());

// after (4.0)
await commandor.ProductService.GetProductByIdAsync(id);
await commandor.ProductService.GetAllProductsAsync(new GetAllProductsQuery());
```

`SendAsync`, `GetAsync`, `Invalidate`, and `InvalidateAsync` on `ICommandor` are unchanged.

---

## Method Naming Conventions

| Type | Recommended prefixes |
|---|---|
| Commands | `Create`, `Update`, `Delete`, `Process`, `Send` |
| Queries | `Get`, `Find`, `List`, `Search` |

---

## License

MIT
