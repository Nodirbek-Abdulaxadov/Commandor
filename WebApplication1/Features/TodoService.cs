using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Features;

/// <summary>
/// Read-only implementation. No commands here — all writes flow through
/// the dedicated <see cref="IRequestHandler{TRequest, TResponse}"/> classes
/// (<see cref="CreateTodoCommandHandler"/> and friends).
/// </summary>
public class TodoService(AppDbContext dbContext) : ITodoService
{
    public virtual Task<List<Todo>> GetAllAsync(CancellationToken ct = default) =>
        dbContext.Todos.ToListAsync(ct);

    public virtual Task<Todo?> GetTodoByIdAsync(int id, CancellationToken ct = default) =>
        dbContext.Todos.FirstOrDefaultAsync(t => t.Id == id, ct);
}
