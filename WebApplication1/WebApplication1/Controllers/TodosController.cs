using Commandor;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Features;

namespace WebApplication1.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TodosController(ICommandor commandor) : ControllerBase
{
    [HttpGet]
    public Task<List<TodoEntity>> GetAllTodos(CancellationToken cancellationToken = default)
    {
        return commandor.SendAsync(new GetAllTodosQuery(), cancellationToken);
    }

    [HttpGet("{id:long}")]
    public Task<TodoEntity?> GetTodo(long id, CancellationToken cancellationToken = default)
    {
        return commandor.SendAsync(new GetTodoQuery(id), cancellationToken);
    }

    [HttpPost]
    public Task<TodoEntity> CreateTodo([FromBody] CreateTodoCommand command, CancellationToken cancellationToken = default)
    {
        return commandor.SendAsync(command, cancellationToken);
    }

    [HttpPut]
    public Task UpdateTodo([FromBody] TodoEntity entity, CancellationToken cancellationToken = default)
    {
        return commandor.SendAsync(new UpdateTodoCommand(entity), cancellationToken);
    }
}