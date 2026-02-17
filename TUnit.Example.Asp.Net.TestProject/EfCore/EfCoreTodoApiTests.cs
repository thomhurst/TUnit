using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TUnit.Example.Asp.Net.Models;

namespace TUnit.Example.Asp.Net.TestProject.EfCore;

/// <summary>
/// Integration tests for the EF Core Todo API demonstrating per-test schema isolation.
/// Each test gets its own schema within the shared PostgreSQL container.
/// Compare with <see cref="TodoApiTests"/> which uses raw SQL with per-test table names.
/// </summary>
public class EfCoreTodoApiTests : EfCoreTodoTestBase
{
    [Test]
    public async Task GetTodos_WhenEmpty_ReturnsEmptyList()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/ef/todos");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var todos = await response.Content.ReadFromJsonAsync<List<Todo>>();
        await Assert.That(todos).IsNotNull();
        await Assert.That(todos!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CreateTodo_ReturnsCreatedTodo()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/ef/todos", new { Title = "EF Core Todo" });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var todo = await response.Content.ReadFromJsonAsync<Todo>();
        await Assert.That(todo).IsNotNull();
        await Assert.That(todo!.Title).IsEqualTo("EF Core Todo");
        await Assert.That(todo.IsComplete).IsFalse();
    }

    [Test]
    public async Task CreateTodo_CanBeVerifiedViaDbContext()
    {
        var client = Factory.CreateClient();

        await client.PostAsJsonAsync("/ef/todos", new { Title = "Verify via EF" });

        // Verify directly via EF Core DbContext
        await using var scope = CreateDbScope(out var dbContext);
        var todo = await dbContext.Todos.SingleAsync();
        await Assert.That(todo.Title).IsEqualTo("Verify via EF");
    }

    [Test]
    public async Task GetTodos_ReturnsSeededData()
    {
        // Seed via EF Core directly
        await SeedTodosAsync("Seeded Item 1", "Seeded Item 2");

        // Verify via API
        var client = Factory.CreateClient();
        var todos = await client.GetFromJsonAsync<List<Todo>>("/ef/todos");
        await Assert.That(todos!.Count).IsEqualTo(2);
    }

    [Test]
    public async Task UpdateTodo_ChangesValues()
    {
        var client = Factory.CreateClient();

        // Create
        var createResponse = await client.PostAsJsonAsync("/ef/todos", new { Title = "Update Me" });
        var created = await createResponse.Content.ReadFromJsonAsync<Todo>();

        // Update
        var updateResponse = await client.PutAsJsonAsync(
            $"/ef/todos/{created!.Id}",
            new { Title = "Updated", IsComplete = true });

        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<Todo>();
        await Assert.That(updated!.Title).IsEqualTo("Updated");
        await Assert.That(updated.IsComplete).IsTrue();
    }

    [Test]
    public async Task DeleteTodo_RemovesTodo()
    {
        var client = Factory.CreateClient();

        // Create
        var createResponse = await client.PostAsJsonAsync("/ef/todos", new { Title = "Delete Me" });
        var created = await createResponse.Content.ReadFromJsonAsync<Todo>();

        // Delete
        var deleteResponse = await client.DeleteAsync($"/ef/todos/{created!.Id}");
        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        // Verify gone
        var getResponse = await client.GetAsync($"/ef/todos/{created.Id}");
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test, Repeat(3)]
    public async Task ParallelTests_AreIsolated()
    {
        // Each repeat gets its own schema - no data leaks between parallel tests
        await using var scope = CreateDbScope(out var dbContext);
        var count = await dbContext.Todos.CountAsync();
        await Assert.That(count).IsEqualTo(0);
    }
}
