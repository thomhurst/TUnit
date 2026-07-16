using System.Net;
using System.Net.Http.Json;

namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// These tests demonstrate that parallel tests have isolated Redis key spaces.
/// Each test has its own key prefix, so data from one test cannot leak into another.
/// The [Repeat(3)] attribute creates multiple iterations that run in parallel,
/// and each gets its own key prefix based on the unique TestContext.Id.
/// </summary>
public class RedisIsolationTests : RedisTestBase
{
    /// <summary>
    /// Sets a value and retrieves it. Each repetition should see only its own value.
    /// If isolation failed, we might see values from other tests.
    /// </summary>
    [Test, Repeat(3)]
    public async Task SetAndGet_ReturnsOwnValue()
    {
        var client = Factory.CreateClient();

        // Set a value
        await client.PostAsJsonAsync("/cache/mykey", new { Value = "my-isolated-value" });

        // Get the value back
        var response = await client.GetAsync("/cache/mykey");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var value = await response.Content.ReadAsStringAsync();
        await Assert.That(value).IsEqualTo("\"my-isolated-value\"");
    }

    /// <summary>
    /// Gets a key that was never set. Each test starts with an empty key space.
    /// </summary>
    [Test, Repeat(3)]
    public async Task Get_WhenKeyDoesNotExist_Returns404()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/cache/nonexistent");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Deletes a key after setting it.
    /// </summary>
    [Test, Repeat(3)]
    public async Task Delete_RemovesKey()
    {
        var client = Factory.CreateClient();

        // Set a value
        await client.PostAsJsonAsync("/cache/to-delete", new { Value = "delete-me" });

        // Delete it
        var deleteResponse = await client.DeleteAsync("/cache/to-delete");
        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        // Verify it's gone
        var getResponse = await client.GetAsync("/cache/to-delete");
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Sets multiple keys. Each repetition should have its own isolated set of keys.
    /// </summary>
    [Test, Repeat(3)]
    public async Task SetMultipleKeys_AllIsolated()
    {
        var client = Factory.CreateClient();

        // Set 3 different keys
        await client.PostAsJsonAsync("/cache/key1", new { Value = "value1" });
        await client.PostAsJsonAsync("/cache/key2", new { Value = "value2" });
        await client.PostAsJsonAsync("/cache/key3", new { Value = "value3" });

        // Verify all exist with correct values
        var response1 = await client.GetAsync("/cache/key1");
        var response2 = await client.GetAsync("/cache/key2");
        var response3 = await client.GetAsync("/cache/key3");

        await Assert.That(response1.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response2.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response3.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var value1 = await response1.Content.ReadAsStringAsync();
        var value2 = await response2.Content.ReadAsStringAsync();
        var value3 = await response3.Content.ReadAsStringAsync();

        await Assert.That(value1).IsEqualTo("\"value1\"");
        await Assert.That(value2).IsEqualTo("\"value2\"");
        await Assert.That(value3).IsEqualTo("\"value3\"");
    }
}
