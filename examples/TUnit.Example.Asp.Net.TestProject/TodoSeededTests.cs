using System.Net;
using System.Net.Http.Json;
using TUnit.Example.Asp.Net.Models;

namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// Example tests using the TodoTestBase for seeded data scenarios.
/// Demonstrates using SeedTodosAsync to set up test preconditions.
/// </summary>
public class TodoSeededTests : TodoTestBase
{
    [Test]
    public async Task GetAll_WithSeededData_ReturnsAll()
    {
        // Seed data directly to database (bypassing API)
        await SeedTodosAsync("Todo 1", "Todo 2", "Todo 3");

        var client = Factory.CreateClient();
        var todos = await client.GetFromJsonAsync<List<Todo>>("/todos");

        await Assert.That(todos!.Count).IsEqualTo(3);
    }

    [Test]
    public async Task GetAll_WithMixedCompletionStatus_ReturnsAll()
    {
        // Seed mixed data
        await SeedTodosAsync("Incomplete 1", "Incomplete 2");
        await SeedCompletedTodoAsync("Completed 1");

        var client = Factory.CreateClient();
        var todos = await client.GetFromJsonAsync<List<Todo>>("/todos");

        await Assert.That(todos!.Count).IsEqualTo(3);
        await Assert.That(todos.Count(t => t.IsComplete)).IsEqualTo(1);
        await Assert.That(todos.Count(t => !t.IsComplete)).IsEqualTo(2);
    }

    [Test]
    public async Task Delete_FromSeededData_RemovesOne()
    {
        // Seed data
        await SeedTodosAsync("Keep 1", "Delete Me", "Keep 2");

        var client = Factory.CreateClient();

        // Get all to find the one to delete
        var todos = await client.GetFromJsonAsync<List<Todo>>("/todos");
        var toDelete = todos!.First(t => t.Title == "Delete Me");

        // Delete
        var deleteResponse = await client.DeleteAsync($"/todos/{toDelete.Id}");
        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        // Verify count via direct database access
        var count = await GetTodoCountAsync();
        await Assert.That(count).IsEqualTo(2);
    }

    [Test]
    public async Task DatabaseAndApiInSync()
    {
        var client = Factory.CreateClient();

        // Create via API
        await client.PostAsJsonAsync("/todos", new { Title = "API Created" });
        await client.PostAsJsonAsync("/todos", new { Title = "API Created 2" });

        // Seed directly
        await SeedTodosAsync("DB Seeded");

        // Both API and database should see all 3
        var apiCount = (await client.GetFromJsonAsync<List<Todo>>("/todos"))!.Count;
        var dbCount = await GetTodoCountAsync();

        await Assert.That(apiCount).IsEqualTo(3);
        await Assert.That(dbCount).IsEqualTo(3);
    }
}
