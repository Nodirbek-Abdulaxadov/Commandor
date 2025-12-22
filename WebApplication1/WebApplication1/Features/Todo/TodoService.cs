namespace WebApplication1.Features;

public class TodoService : ITodoService
{
    public virtual Task<List<TodoEntity>> GetAll(GetAllTodosQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(TodoDB.Todos);
    }

    public virtual Task<TodoEntity?> GetById(GetTodoQuery query, CancellationToken cancellationToken = default)
    {
        var todo = TodoDB.Todos.FirstOrDefault(t => t.Id == query.Id);
        return Task.FromResult(todo);
    }

    public virtual Task<TodoEntity> Create(CreateTodoCommand command, CancellationToken cancellationToken = default)
    {
        TodoDB.Todos.Add(new TodoEntity
        {
            Id = TodoDB.Todos.Count + 1,
            Task = command.Task,
            IsDone = false
        });

        return Task.FromResult(TodoDB.Todos.Last());
    }

    public virtual Task Update(UpdateTodoCommand command, CancellationToken cancellationToken = default)
    {
        var todo = TodoDB.Todos.FirstOrDefault(t => t.Id == command.Entity.Id);
        if (todo != null)
        if (todo != null)
        {
            todo.Task = command.Entity.Task;
            todo.IsDone = command.Entity.IsDone;
        }
        return Task.CompletedTask;
    }
}

public static class TodoDB
{
    public static List<TodoEntity> Todos { get; } = new();
}