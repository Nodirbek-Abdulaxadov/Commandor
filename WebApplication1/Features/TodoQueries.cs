using Commandor;

namespace WebApplication1.Features;

public record GetAllTodosQuery() : IRequest<List<Todo>>;

public record GetTodoByIdQuery(int Id) : IRequest<Todo?>;