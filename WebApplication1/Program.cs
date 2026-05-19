using WebApplication1.Data;
using WebApplication1.Features;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add DbContext
builder.Services.AddDbContext<AppDbContext>();

// Add Commandor with auto-generated handlers
builder.Services.AddCommandor();
builder.Services.AddCommandorService<ITodoService, TodoService>();

// Add NSwag OpenAPI/Swagger
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "Todo API with Commandor";
    config.Version = "v1";
    config.Description = "RESTful API built with Commandor (MediatR alternative) and NSwag";
    config.DocumentName = "v1";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // NSwag Swagger UI
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentPath = "/swagger/v1/swagger.json";
        config.Path = "/swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
