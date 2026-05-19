using Commandor;

namespace WebApplication1.Features;

/// <summary>
/// Query-only surface for todos. Every method is a cacheable read — the
/// source generator wraps this interface in a <c>TodoServiceCachedProxy</c>
/// behind the scenes. Writes live in dedicated command handlers
/// (see <c>CreateTodoCommandHandler</c> etc.) so cache invalidation has
/// a clear home.
/// </summary>
public interface ITodoService : ICommandorService
{
    [QueryHandler]
    Task<Todo?> GetTodoByIdAsync(int id, CancellationToken ct = default);

    [QueryHandler]
    Task<List<Todo>> GetAllAsync(CancellationToken ct = default);
}
