# Commandor

[![NuGet](https://img.shields.io/nuget/v/ICommandor.svg)](https://www.nuget.org/packages/ICommandor/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)

A lightweight CQRS/Mediator for .NET. **One injection point, two
patterns**: send commands as instances, call queries as methods on
auto-generated service properties.

## Install

```bash
dotnet add package ICommandor
```

## 60-second tour

```csharp
// 1.  Define a query-only service interface. Each method is cached.
public interface ITodoService : ICommandorService
{
    [QueryHandler(CacheTtlSeconds = 60)]
    Task<TodoItem?> GetByIdAsync(int id, CancellationToken ct = default);

    [QueryHandler]
    Task<List<TodoItem>> ListAsync(string? filter = null, CancellationToken ct = default);
}

// 2.  Implement it as a plain class. Reads only — no caching code here.
public class TodoService(AppDbContext db) : ITodoService
{
    public Task<TodoItem?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Todos.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<List<TodoItem>> ListAsync(string? filter = null, CancellationToken ct = default) =>
        db.Todos.Where(t => filter == null || t.Title.Contains(filter)).ToListAsync(ct);
}

// 3.  Write each command as an explicit IRequest record + handler.
//     The handler invalidates affected query caches when its write commits.
public sealed record CreateTodoCommand(string Title) : IRequest<TodoItem>;

public sealed class CreateTodoHandler(AppDbContext db, ICommandor commandor)
    : IRequestHandler<CreateTodoCommand, TodoItem>
{
    public async Task<TodoItem> HandleAsync(CreateTodoCommand cmd, CancellationToken ct = default)
    {
        var todo = new TodoItem { Title = cmd.Title };
        db.Todos.Add(todo);
        await db.SaveChangesAsync(ct);
        commandor.Invalidate<ITodoService>();   // ← manual, explicit
        return todo;
    }
}

// 4.  Register.
builder.Services.AddCommandor<Program>();
builder.Services.AddCommandorService<ITodoService, TodoService>();
builder.Services.AddAppCommandor();   // source-generated; only exists when at least one [QueryHandler] is present

// 5.  Use it from a controller — one injected object covers both paths.
public class TodosController(AppCommandor commandor) : ControllerBase
{
    [HttpGet("{id}")]
    public Task<TodoItem?> Get(int id) =>
        commandor.TodoService.GetByIdAsync(id);                       // cached query

    [HttpGet]
    public Task<List<TodoItem>> List(string? q) =>
        commandor.TodoService.ListAsync(q);                           // cached query

    [HttpPost]
    public Task<TodoItem> Add([FromBody] CreateTodoCommand cmd) =>
        commandor.SendAsync(cmd);                                     // command (instance)
}
```

The first `GetByIdAsync(7)` hits the database; the second returns the
cached result. `SendAsync(new CreateTodoCommand(...))` runs the matching
handler, which calls `commandor.Invalidate<ITodoService>()` and the next
read repopulates the cache.

## Two patterns, one mediator

| | Commands | Queries |
|---|---|---|
| Where the contract lives | a record class (`CreateTodoCommand`) implementing `IRequest<T>` | a `[QueryHandler]` method on an `ICommandorService` interface |
| Where the work lives | a hand-written `IRequestHandler<TRequest, TResponse>` class | the service implementation |
| Call site | `commandor.SendAsync(cmd)` | `commandor.TodoService.SomeMethod(...)` |
| Caching | not cached — every call dispatches | transparently memoized by the generated proxy |
| Invalidation | the command handler decides when to call `commandor.Invalidate<TService>()` | n/a (driven by command handlers) |

This is deliberate: the parts of CQRS that *should* be loud — the
named-intent objects that flow through your system — are explicit, and
the parts that *should* be quiet — boring reads — disappear into a
single property access.

## Caching details

- Storage: `IMemoryCache`.
- Cache key: `ServiceType.MethodName(arg1, arg2, …)`. Primitive and
  `Guid` / `DateTime{Offset}` arguments contribute their values; complex
  arguments contribute `TypeName#GetHashCode()`. Records work
  out-of-the-box because their `GetHashCode()` is value-based.
- Invalidation: `CommandorContext` keeps one `CancellationTokenSource`
  per service type; every cached entry depends on its token, and
  `commandor.Invalidate<TService>()` cancels it.
- TTL: per-method `[QueryHandler(CacheTtlSeconds = N)]`. Default is
  "until invalidated or evicted".

## What the source generator emits

Two artefacts per project:

1. **`FooServiceCachedProxy`** — one per service interface with at least
   one `[QueryHandler]` method. Wraps the implementation: query methods
   read/write `IMemoryCache`; non-query methods pass through.
2. **`AppCommandor`** (plus `IServiceCollection.AddAppCommandor()`) —
   one per project, exposes every `ICommandorService` as a property on
   the inherited `Commandor` mediator.

Both live in the `Commandor.Generated` namespace.

## Migrating from v2.x

v2 routed both commands and queries through cached service proxies via
`[CommandHandler]` / `[QueryHandler]` on the same interface. v3 splits
the two:

1. Drop `[CommandHandler]` everywhere — the attribute is gone.
2. For each former `[CommandHandler]` method, define an explicit
   `IRequest<T>` record and a hand-written `IRequestHandler<T, R>`
   class. Call `commandor.Invalidate<TService>()` inside the handler.
3. Replace direct `ITodoService` injections with `AppCommandor` and
   access queries as properties: `commandor.TodoService.SomeMethod(...)`.
4. Add `services.AddAppCommandor()` next to `AddCommandor()`.
5. Stop using `commandor.GetAsync(...)` — the alias is removed.

## License

MIT — see [LICENSE.txt](LICENSE.txt).
