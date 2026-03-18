using Commandor;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Features;

public class TodoService(AppDbContext dbContext, ICommandor commandor) : ITodoService
{
    public virtual async Task<List<Todo>> GetAllTodosAsync(GetAllTodosQuery query, CancellationToken ct = default)
    {
        return await dbContext.Todos.ToListAsync(ct);
    }

    public virtual async Task<Todo?> GetTodoByIdAsync(int id, CancellationToken ct = default)
    {
        return await dbContext.Todos.FindAsync([id], ct);
    }

    public virtual async Task<Todo> CreateTodoAsync(CreateTodoCommand command, CancellationToken ct = default)
    {
        await commandor.InvalidateAsync<ITodoService>(ct);
        var todo = new Todo { Title = command.Title };
        dbContext.Todos.Add(todo);
        await dbContext.SaveChangesAsync(ct);
        return todo;
    }

    public virtual async Task<Todo?> UpdateTodoAsync(UpdateTodoCommand command, CancellationToken ct = default)
    {
        await commandor.InvalidateAsync<ITodoService>(ct);
        var todo = await dbContext.Todos.FindAsync([command.Id], ct);
        if (todo == null)
            return null;
        
        todo.Title = command.Title;
        todo.IsCompleted = command.IsCompleted;
        await dbContext.SaveChangesAsync(ct);
        return todo;
    }

    public virtual async Task<bool> DeleteTodoAsync(DeleteTodoCommand command, CancellationToken ct = default)
    {
        await commandor.InvalidateAsync<ITodoService>(ct);
        var todo = await dbContext.Todos.FindAsync([command.Id], ct);
        if (todo == null)
            return false;
        
        dbContext.Todos.Remove(todo);
        await dbContext.SaveChangesAsync(ct);
        return true;
    }
}