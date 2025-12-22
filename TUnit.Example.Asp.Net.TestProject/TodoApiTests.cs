using System.Net;
using System.Net.Http.Json;
using TUnit.AspNetCore;
using TUnit.Example.Asp.Net.Models;

namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// Integration tests for the Todo API demonstrating per-test table isolation.
/// Each test gets its own table within the shared PostgreSQL container.
/// </summary>
public class TodoApiTests : TodoTestBase
{
    [Test]
    public async Task GetTodos_WhenEmpty_ReturnsEmptyList()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/todos");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var todos = await response.Content.ReadFromJsonAsync<List<Todo>>();
        await Assert.That(todos).IsNotNull();
        await Assert.That(todos!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CreateTodo_ReturnsCreatedTodo()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/todos", new { Title = "Test Todo" });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var todo = await response.Content.ReadFromJsonAsync<Todo>();
        await Assert.That(todo).IsNotNull();
        await Assert.That(todo!.Title).IsEqualTo("Test Todo");
        await Assert.That(todo.IsComplete).IsFalse();
    }

    [Test]
    public async Task GetTodo_AfterCreate_ReturnsTodo()
    {
        var client = Factory.CreateClient();

        // Create
        var createResponse = await client.PostAsJsonAsync("/todos", new { Title = "Get Me" });
        var created = await createResponse.Content.ReadFromJsonAsync<Todo>();

        // Get
        var getResponse = await client.GetAsync($"/todos/{created!.Id}");

        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var todo = await getResponse.Content.ReadFromJsonAsync<Todo>();
        await Assert.That(todo!.Title).IsEqualTo("Get Me");
    }

    [Test]
    public async Task UpdateTodo_ChangesIsComplete()
    {
        var client = Factory.CreateClient();

        // Create
        var createResponse = await client.PostAsJsonAsync("/todos", new { Title = "Update Me" });
        var created = await createResponse.Content.ReadFromJsonAsync<Todo>();

        // Update
        var updateResponse = await client.PutAsJsonAsync(
            $"/todos/{created!.Id}",
            new { Title = "Updated", IsComplete = true });

        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<Todo>();
        await Assert.That(updated!.IsComplete).IsTrue();
        await Assert.That(updated.Title).IsEqualTo("Updated");
    }

    [Test]
    public async Task DeleteTodo_RemovesTodo()
    {
        var client = Factory.CreateClient();

        // Create
        var createResponse = await client.PostAsJsonAsync("/todos", new { Title = "Delete Me" });
        var created = await createResponse.Content.ReadFromJsonAsync<Todo>();

        // Delete
        var deleteResponse = await client.DeleteAsync($"/todos/{created!.Id}");
        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        // Verify gone
        var getResponse = await client.GetAsync($"/todos/{created.Id}");
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetTodo_WhenNotFound_Returns404()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/todos/99999");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreateMultipleTodos_GetAllReturnsThem()
    {
        var client = GlobalFactory.CreateDefaultClient();

        var services = Factory.Services;

        // Create multiple todos
        await client.PostAsJsonAsync("/todos", new { Title = "Todo 1" });
        await client.PostAsJsonAsync("/todos", new { Title = "Todo 2" });
        await client.PostAsJsonAsync("/todos", new { Title = "Todo 3" });

        // Get all
        var response = await client.GetAsync("/todos");
        var todos = await response.Content.ReadFromJsonAsync<List<Todo>>();

        await Assert.That(todos!.Count).IsEqualTo(3);
    }
}
