using Commandor;

namespace WebApplication1.Features;

public record CreateTodoCommand(string Title) : IRequest<Todo>;

public record UpdateTodoCommand(int Id, string Title, bool IsCompleted) : IRequest<Todo>;

public record DeleteTodoCommand(int Id) : IRequest<bool>;