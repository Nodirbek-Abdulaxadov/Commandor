using Commandor;
using WebApplication1.Features;

public record CreateTodoCommand(string Task) : IRequest<TodoEntity>;
public record UpdateTodoCommand(TodoEntity Entity) : IRequest;

// Queries (Read operations - auto caching)
public record GetAllTodosQuery() : IRequest<List<TodoEntity>>;
public record GetTodoQuery(long Id) : IRequest<TodoEntity?>;
