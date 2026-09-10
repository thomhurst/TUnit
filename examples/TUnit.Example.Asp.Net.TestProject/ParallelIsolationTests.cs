using System.Net.Http.Json;
using TUnit.Example.Asp.Net.Models;

namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// These tests demonstrate that parallel tests are completely isolated.
/// Each test has its own table, so data from one test cannot leak into another.
/// The [Repeat(3)] attribute creates multiple iterations that run in parallel,
/// and each gets its own table based on the unique TestContext.Id.
/// TUnit runs tests in parallel by default, so no attribute is needed.
/// </summary>
public class ParallelIsolationTests : TodoTestBase
{
    /// <summary>
    /// Creates 5 todos. Each of the 3 repetitions should see exactly 5.
    /// If isolation failed, we'd see 10 or 15.
    /// </summary>
    [Test, Repeat(3)]
    public async Task Test1_CreateFiveTodos_ReturnsExactlyFive()
    {
        var client = Factory.CreateClient();

        // Create 5 todos
        for (var i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/todos", new { Title = $"Parallel Todo {i}" });
        }

        // Verify exactly 5 (not 10 or 15 - isolation works!)
        var todos = await client.GetFromJsonAsync<List<Todo>>("/todos");
        await Assert.That(todos!.Count).IsEqualTo(5);
    }

    /// <summary>
    /// Creates 3 todos with different titles. Each repetition should see exactly 3.
    /// </summary>
    [Test, Repeat(3)]
    public async Task Test2_CreateThreeTodos_ReturnsExactlyThree()
    {
        var client = Factory.CreateClient();

        // Create 3 todos
        for (var i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync("/todos", new { Title = $"Different {i}" });
        }

        // Verify exactly 3 (not 6, not 9 - isolation works!)
        var todos = await client.GetFromJsonAsync<List<Todo>>("/todos");
        await Assert.That(todos!.Count).IsEqualTo(3);
    }

    /// <summary>
    /// Each repetition starts with an empty table. If isolation failed,
    /// we'd see data from previous repetitions.
    /// </summary>
    [Test, Repeat(3)]
    public async Task Test3_StartsEmpty_ReturnZeroTodos()
    {
        var client = Factory.CreateClient();

        // Fresh table should be empty
        var todos = await client.GetFromJsonAsync<List<Todo>>("/todos");
        await Assert.That(todos!.Count).IsEqualTo(0);
    }

    /// <summary>
    /// Creates a todo and verifies it can be retrieved. Each repetition
    /// should have its own isolated todo.
    /// </summary>
    [Test, Repeat(3)]
    public async Task Test4_CreateAndRetrieve_Works()
    {
        var client = Factory.CreateClient();

        // Create
        var createResponse = await client.PostAsJsonAsync("/todos", new { Title = "Isolated Todo" });
        var created = await createResponse.Content.ReadFromJsonAsync<Todo>();

        // Retrieve
        var retrieved = await client.GetFromJsonAsync<Todo>($"/todos/{created!.Id}");

        await Assert.That(retrieved!.Title).IsEqualTo("Isolated Todo");
        await Assert.That(retrieved.Id).IsEqualTo(created.Id);
    }
}
