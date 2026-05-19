using Commandor;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Features;

/// <summary>
/// Each command has its own handler. The handler runs the write,
/// then calls <c>commandor.Invalidate&lt;ITodoService&gt;()</c> so the
/// next read goes back to the database.
/// </summary>
public sealed class CreateTodoCommandHandler(AppDbContext db, ICommandor commandor)
    : IRequestHandler<CreateTodoCommand, Todo>
{
    public async Task<Todo> HandleAsync(CreateTodoCommand command, CancellationToken ct = default)
    {
        var todo = new Todo { Title = command.Title };
        db.Todos.Add(todo);
        await db.SaveChangesAsync(ct);
        commandor.Invalidate<ITodoService>();
        return todo;
    }
}

public sealed class UpdateTodoCommandHandler(AppDbContext db, ICommandor commandor)
    : IRequestHandler<UpdateTodoCommand, Todo?>
{
    public async Task<Todo?> HandleAsync(UpdateTodoCommand command, CancellationToken ct = default)
    {
        var todo = await db.Todos.FindAsync([command.Id], ct);
        if (todo is null) return null;

        todo.Title = command.Title;
        todo.IsCompleted = command.IsCompleted;
        await db.SaveChangesAsync(ct);
        commandor.Invalidate<ITodoService>();
        return todo;
    }
}

public sealed class DeleteTodoCommandHandler(AppDbContext db, ICommandor commandor)
    : IRequestHandler<DeleteTodoCommand, bool>
{
    public async Task<bool> HandleAsync(DeleteTodoCommand command, CancellationToken ct = default)
    {
        var todo = await db.Todos.FindAsync([command.Id], ct);
        if (todo is null) return false;

        db.Todos.Remove(todo);
        await db.SaveChangesAsync(ct);
        commandor.Invalidate<ITodoService>();
        return true;
    }
}
