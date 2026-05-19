using Commandor;

namespace WebApplication1.Features;

// Empty-param query still needs an IRequest wrapper record.
public record GetAllTodosQuery() : IRequest<List<Todo>>;

// GetTodoByIdQuery removed — the source generator auto-creates an internal
// wrapper record for  Task<Todo?> GetTodoByIdAsync(int id, ...) in plain-type mode.
// Call site:  await commandor.GetTodoByIdAsync(id)  (generated extension method)
