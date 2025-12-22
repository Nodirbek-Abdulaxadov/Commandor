using Commandor;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Features;

namespace WebApplication1.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ColorsController(ICommandor commandor) : ControllerBase
{
    [HttpGet]
    public Task<List<ColorEntity>> GetAllColors(CancellationToken cancellationToken = default)
    {
        return commandor.SendAsync(new GetAllColorsQuery(), cancellationToken);
    }
    [HttpGet("{id:long}")]
    public Task<ColorEntity?> GetColor(long id, CancellationToken cancellationToken = default)
    {
        return commandor.SendAsync(new GetColorQuery(id), cancellationToken);
    }
    [HttpPost]
    public Task<ColorEntity> CreateColor([FromBody] CreateColorCommand command, CancellationToken cancellationToken = default)
    {
        return commandor.SendAsync(command, cancellationToken);
    }
    [HttpPut]
    public Task UpdateColor([FromBody] ColorEntity entity, CancellationToken cancellationToken = default)
    {
        return commandor.SendAsync(new UpdateColorCommand(entity), cancellationToken);
    }
}
