using Commandor;
using NSwag.AspNetCore;
using NSwag.Generation.AspNetCore;
using WebApplication1.Features;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApiDocument(options => options.Title = "Todos API");
builder.Services.AddCommandor();
builder.Services.AddCommandorService<ITodoService, TodoService>();
builder.Services.AddCommandorService<IColorService, ColorService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
