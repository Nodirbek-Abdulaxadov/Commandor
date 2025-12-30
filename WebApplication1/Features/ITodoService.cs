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
    
    [QueryHandler]
    Task<List<Todo>> GetAllTodosAsync(GetAllTodosQuery query, CancellationToken ct = default);
    
    [QueryHandler]
    Task<Todo?> GetTodoByIdAsync(GetTodoByIdQuery query, CancellationToken ct = default);

    Task InvalidateAsync(CancellationToken cancellationToken = default);
}