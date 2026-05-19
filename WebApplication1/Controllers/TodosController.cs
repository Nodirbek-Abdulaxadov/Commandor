using Commandor.Generated;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Features;

namespace WebApplication1.Controllers;

/// <summary>
/// Todo CRUD operations.
///
/// Queries flow through the generated <see cref="AppCommandor.TodoService"/>
/// property (cached, read-locked behind the scenes). Commands flow through
/// <c>SendAsync</c> with an explicit request record. Cache invalidation
/// lives in the command handlers themselves.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class TodosController(AppCommandor commandor) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Todo), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTodo([FromBody] CreateTodoCommand command)
    {
        var todo = await commandor.SendAsync(command);
        return CreatedAtAction(nameof(GetTodoById), new { id = todo.Id }, todo);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Todo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTodoById(int id)
    {
        var todo = await commandor.TodoService.GetTodoByIdAsync(id);
        return todo is null ? NotFound() : Ok(todo);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<Todo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTodos()
    {
        var todos = await commandor.TodoService.GetAllAsync();
        return Ok(todos);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Todo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTodo(int id, [FromBody] UpdateTodoCommand command)
    {
        if (id != command.Id) return BadRequest();
        var todo = await commandor.SendAsync(command);
        return todo is null ? NotFound() : Ok(todo);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTodo(int id)
    {
        var success = await commandor.SendAsync(new DeleteTodoCommand(id));
        return success ? NoContent() : NotFound();
    }
}
