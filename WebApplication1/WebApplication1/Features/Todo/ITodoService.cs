using Commandor;

namespace WebApplication1.Features;

public interface ITodoService : ICommandorService
{
    [QueryHandler]
    Task<List<TodoEntity>> GetAll(GetAllTodosQuery query, CancellationToken cancellationToken = default);

    [QueryHandler]
    Task<TodoEntity?> GetById(GetTodoQuery query, CancellationToken cancellationToken = default);

    [CommandHandler]
    Task<TodoEntity> Create(CreateTodoCommand command, CancellationToken cancellationToken = default);

    [CommandHandler]
    Task Update(UpdateTodoCommand command, CancellationToken cancellationToken = default);
}
