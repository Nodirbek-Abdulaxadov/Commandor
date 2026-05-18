# Changelog

All notable changes to this project are documented in this file.

## [2.0.0] — Unreleased

### Added — Direct-injection caching (Fusion-style)

Inject the service interface itself and the source generator does the rest:

```csharp
public class TodosController(ITodoService todos) : ControllerBase
{
    [HttpGet("{id}")]
    public Task<TodoItem?> Get(int id) => todos.GetByIdAsync(id);
    //                                    ↑ auto-cached, no mediator boilerplate
}
```

For every interface that derives from `ICommandorService`, the generator now emits a `[GeneratedProxy(typeof(TService))]` class that implements the same interface and:
- **`[QueryHandler]` methods** — wrap the call in an `IMemoryCache` lookup keyed by the argument values. Same `CacheTtlSeconds` semantics as before.
- **`[CommandHandler]` methods** — invoke the real implementation, then automatically call `CommandorContext.Invalidate(typeof(TService))` so subsequent queries see fresh data. **No more manual `await commandor.InvalidateAsync<TService>()` inside command bodies.**
- **Undecorated methods** — pure pass-through, no behaviour change.

`AddCommandorService<TService, TImpl>()` wires the proxy as the resolution of `TService` and registers the concrete `TImpl` as scoped so the proxy can call into it.

### Kept — Legacy mediator path

`commandor.SendAsync(...)` / `commandor.GetAsync(...)` and the per-method `commandor.MethodNameAsync(...)` extension methods all still work — they're emitted alongside the new proxy. Existing applications upgrade without code changes.

### Changed

- **Versions unified** under `Directory.Build.props` (`Commandor` and `Commandor.Generators` now ship the same version).
- `Microsoft.CodeAnalysis.CSharp` bumped to **4.11.0** for faster incremental generation in modern IDEs.
- Test stack bumped: `xunit` 2.5.3 → **2.9.2**, `Microsoft.NET.Test.Sdk` 17.8.0 → **17.12.0**, `coverlet.collector` 6.0.0 → **6.0.2**, `xunit.runner.visualstudio` 2.5.3 → **2.8.2**. Dropped the accidental `Microsoft.Extensions.DependencyInjection 10.0.0` (preview) pin.
- `QueryHandlerAttribute` is now `sealed`, matching `CommandHandlerAttribute`. The `CacheTtlSeconds` doc no longer claims it's "decorative" — it has been wired up since 1.1.

### Added — Tooling

- `Directory.Build.props` (shared version + deterministic builds).
- `CachedProxyTests` covering query memoization, command auto-invalidation, default parameter values, and untracked-method pass-through.
- `CHANGELOG.md`.
