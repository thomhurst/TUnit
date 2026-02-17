using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TUnit.Example.Asp.Net.Configuration;
using TUnit.Example.Asp.Net.EfCore;
using TUnit.Example.Asp.Net.Models;
using TUnit.Example.Asp.Net.Repositories;
using TUnit.Example.Asp.Net.Services;

var builder = WebApplication.CreateBuilder(args);

if (builder.Configuration["SomeKey"] != "SomeValue")
{
    throw new InvalidOperationException("SomeKey is not SomeValue - But we set it in WebApplicationFactory");
}

builder.Services.AddOpenApi();
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));
builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.SectionName));
builder.Services.AddScoped<ITodoRepository, TodoRepository>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// EF Core DbContext - reads connection string and schema from configuration.
// The DbContext constructor reads Database:Schema from IConfiguration directly.
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseNpgsql(builder.Configuration["Database:ConnectionString"] ?? "")
        .ReplaceService<IModelCacheKeyFactory, SchemaModelCacheKeyFactory>());

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Endpoints");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/ping", () =>
{
    logger.LogInformation("Ping endpoint called");
    return "Hello, World!";
});

// Todo CRUD endpoints (raw SQL)
app.MapGet("/todos", async (ITodoRepository repo) =>
    await repo.GetAllAsync());

app.MapGet("/todos/{id:int}", async (int id, ITodoRepository repo) =>
    await repo.GetByIdAsync(id) is { } todo
        ? Results.Ok(todo)
        : Results.NotFound());

app.MapPost("/todos", async (CreateTodoRequest request, ITodoRepository repo) =>
{
    var todo = await repo.CreateAsync(new Todo { Title = request.Title });
    return Results.Created($"/todos/{todo.Id}", todo);
});

app.MapPut("/todos/{id:int}", async (int id, UpdateTodoRequest request, ITodoRepository repo) =>
    await repo.UpdateAsync(id, new Todo { Title = request.Title, IsComplete = request.IsComplete }) is { } todo
        ? Results.Ok(todo)
        : Results.NotFound());

app.MapDelete("/todos/{id:int}", async (int id, ITodoRepository repo) =>
    await repo.DeleteAsync(id)
        ? Results.NoContent()
        : Results.NotFound());

// Cache endpoints (Redis)
app.MapGet("/cache/{key}", async (string key, ICacheService cache) =>
    await cache.GetAsync(key) is { } value
        ? Results.Ok(value)
        : Results.NotFound());

app.MapPost("/cache/{key}", async (string key, CacheValueRequest request, ICacheService cache) =>
{
    await cache.SetAsync(key, request.Value);
    return Results.Created($"/cache/{key}", request.Value);
});

app.MapDelete("/cache/{key}", async (string key, ICacheService cache) =>
    await cache.DeleteAsync(key)
        ? Results.NoContent()
        : Results.NotFound());

// EF Core Todo endpoints (Code First approach alongside raw SQL above)
// TodoDbContext reads Database:Schema from IConfiguration in its constructor,
// so no manual schema wiring is needed in each endpoint.
app.MapGet("/ef/todos", async (TodoDbContext db) =>
    await db.Todos.OrderByDescending(t => t.CreatedAt).ToListAsync());

app.MapGet("/ef/todos/{id:int}", async (int id, TodoDbContext db) =>
    await db.Todos.FindAsync(id) is { } todo
        ? Results.Ok(todo)
        : Results.NotFound());

app.MapPost("/ef/todos", async (CreateTodoRequest request, TodoDbContext db) =>
{
    var todo = new Todo { Title = request.Title };
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created($"/ef/todos/{todo.Id}", todo);
});

app.MapPut("/ef/todos/{id:int}", async (int id, UpdateTodoRequest request, TodoDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null)
    {
        return Results.NotFound();
    }

    todo.Title = request.Title;
    todo.IsComplete = request.IsComplete;
    await db.SaveChangesAsync();
    return Results.Ok(todo);
});

app.MapDelete("/ef/todos/{id:int}", async (int id, TodoDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null)
    {
        return Results.NotFound();
    }

    db.Todos.Remove(todo);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

public partial class Program;

public record CreateTodoRequest(string Title);
public record UpdateTodoRequest(string Title, bool IsComplete);
public record CacheValueRequest(string Value);
