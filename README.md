# Commandor

A lightweight CQRS/Mediator library for .NET with automatic caching and Roslyn source generation.

## Installation

```bash
dotnet add package ICommandor
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
    // The generator creates the IRequest record and a typed extension method for you.
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
        // Generated extension method — no IRequest wrapper at the call site
        var todo = await commandor.GetTodoByIdAsync(id);
        return todo is null ? NotFound() : Ok(todo);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllTodos()
    {
        // IRequest-mode generated extension
        var todos = await commandor.GetAllTodosAsync(new GetAllTodosQuery());
        return Ok(todos);
    }
}
```

---

## Features

### Plain-Type Query Parameters

For `[QueryHandler]` methods, you can use any parameter types — no need to manually create an `IRequest<T>` record. The source generator creates an internal wrapper record and a public extension method on `ICommandor`:

```csharp
// Interface
[QueryHandler(CacheTtlSeconds = 60)]
Task<Product?> GetProductByIdAsync(int id, CancellationToken ct = default);

// Generated (invisible — lives in generated code):
//   internal sealed record GetProductByIdAsyncRequest(int Id) : IRequest<Product?>;
//   public static Task<Product?> GetProductByIdAsync(this ICommandor c, int id, CancellationToken ct = default)
//       => c.GetAsync(new GetProductByIdAsyncRequest(id), ct);

// Call site — clean, no wrapper record in sight:
var product = await commandor.GetProductByIdAsync(productId);
```

> **Commands always require an `IRequest` record** (plain-type is only for `[QueryHandler]`).

### GetAsync — Semantic Query API

`GetAsync` is a semantic alias for `SendAsync` when dispatching queries. Use `SendAsync` for commands and `GetAsync` for queries — the behaviour is identical, but the intent is clearer.

```csharp
// Both work; GetAsync signals "this is a read operation"
var result1 = await commandor.SendAsync(new GetProductByIdQuery(id));
var result2 = await commandor.GetAsync(new GetProductByIdQuery(id));

// Or via the generated extension method (preferred):
var result3 = await commandor.GetProductByIdAsync(id);
```

All three share the **same cache entry**.

### Auto-Generated Extension Methods

The source generator emits a typed extension method on `ICommandor` for every `[QueryHandler]` — both IRequest-mode and plain-type:

| Interface method | Generated extension |
|---|---|
| `Task<T> GetFooAsync(FooQuery q, ...)` | `commandor.GetFooAsync(new FooQuery(...))` |
| `Task<T> GetBarAsync(int id, ...)` | `commandor.GetBarAsync(id)` |

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

Commandor uses an **incremental Roslyn source generator** (`IIncrementalGenerator`). It generates handler classes and extension methods for every `[CommandHandler]` and `[QueryHandler]` at compile time — no reflection at startup, no boilerplate.

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

Queries are read-only and are automatically cached.

### IRequest-Based (required for zero-param queries)

```csharp
public record GetAllProductsQuery() : IRequest<List<Product>>;

public interface IProductService : ICommandorService
{
    [QueryHandler(CacheTtlSeconds = 60)]
    Task<List<Product>> GetAllProductsAsync(GetAllProductsQuery query, CancellationToken ct = default);
}

// Dispatch via generated extension or GetAsync:
var products = await commandor.GetAllProductsAsync(new GetAllProductsQuery());
```

### Plain-Type (recommended for parameterized queries)

```csharp
public interface IProductService : ICommandorService
{
    [QueryHandler(CacheTtlSeconds = 60)]
    Task<Product?> GetProductByIdAsync(int id, CancellationToken ct = default);
}

// No record to construct — just pass the value:
var product = await commandor.GetProductByIdAsync(42);
```

---

## Method Naming Conventions

| Type | Recommended prefixes |
|---|---|
| Commands | `Create`, `Update`, `Delete`, `Process`, `Send` |
| Queries | `Get`, `Find`, `List`, `Search` |

---

## License

MIT
