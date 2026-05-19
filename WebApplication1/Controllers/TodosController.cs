using Commandor;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Features;

namespace WebApplication1.Controllers;

/// <summary>
/// Todo CRUD operations with Commandor pattern
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class TodosController(ICommandor commandor) : ControllerBase
{
    /// <summary>
    /// Create a new todo item
    /// </summary>
    /// <param name="request">Todo creation data</param>
    /// <returns>Created todo item</returns>
    /// <response code="201">Todo successfully created</response>
    /// <response code="400">Invalid request</response>
    [HttpPost]
    [ProducesResponseType(typeof(Todo), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTodo([FromBody] CreateTodoCommand request)
    {
        var todo = await commandor.SendAsync(request);
        return CreatedAtAction(nameof(GetTodoById), new { id = todo.Id }, todo);
    }
    
    /// <summary>
    /// Get a specific todo item by ID
    /// </summary>
    /// <param name="id">Todo ID</param>
    /// <returns>Todo item if found</returns>
    /// <response code="200">Todo found and returned</response>
    /// <response code="404">Todo not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Todo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTodoById(int id)
    {
        // Plain-type extension method — no IRequest wrapper needed at the call site.
        var todo = await commandor.GetTodoByIdAsync(id);
        if (todo == null)
            return NotFound();

        return Ok(todo);
    }

    /// <summary>
    /// Get all todo items
    /// </summary>
    /// <returns>List of all todos</returns>
    /// <response code="200">List of todos returned</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<Todo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTodos()
    {
        // Generated IRequest-mode extension: commandor.GetAllTodosAsync(query)
        var todos = await commandor.GetAllTodosAsync(new GetAllTodosQuery());
        return Ok(todos);
    }
    
    /// <summary>
    /// Update an existing todo item
    /// </summary>
    /// <param name="id">Todo ID</param>
    /// <param name="request">Updated todo data</param>
    /// <returns>Updated todo item</returns>
    /// <response code="200">Todo successfully updated</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">Todo not found</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Todo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTodo(int id, [FromBody] UpdateTodoCommand request)
    {
        if (id != request.Id)
            return BadRequest();
        
        var todo = await commandor.SendAsync(request);
        if (todo == null)
            return NotFound();
        
        return Ok(todo);
    }
    
    /// <summary>
    /// Delete a todo item
    /// </summary>
    /// <param name="id">Todo ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Todo successfully deleted</response>
    /// <response code="404">Todo not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTodo(int id)
    {
        var success = await commandor.SendAsync(new DeleteTodoCommand(id));
        if (!success)
            return NotFound();
        
        return NoContent();
    }
}