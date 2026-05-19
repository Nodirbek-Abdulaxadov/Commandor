using Commandor;

namespace WebApplication1.Features;

public interface ITodoService : ICommandorService
{
    [CommandHandler]
    Task<Todo> CreateTodoAsync(CreateTodoCommand command, CancellationToken ct = default);

    [CommandHandler]
    Task<Todo?> UpdateTodoAsync(UpdateTodoCommand command, CancellationToken ct = default);

    [CommandHandler]
    Task<bool> DeleteTodoAsync(DeleteTodoCommand command, CancellationToken ct = default);

    // IRequest-based query — no parameters, so a wrapper record is still needed.
    [QueryHandler]
    Task<List<Todo>> GetAllTodosAsync(GetAllTodosQuery query, CancellationToken ct = default);

    // Plain-type query — generator auto-creates the wrapper record and a typed
    // ICommandor extension method:  commandor.GetTodoByIdAsync(id)
    [QueryHandler]
    Task<Todo?> GetTodoByIdAsync(int id, CancellationToken ct = default);

    [QueryHandler]
    Task<List<Todo>> GetAllAsync(CancellationToken ct = default);
}