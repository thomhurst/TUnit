namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// Simple integration tests using the WebApplicationTest pattern via TestsBase.
/// These tests don't need per-test isolation (no database writes).
/// </summary>
public class Tests : TestsBase
{
    [Test]
    public async Task Ping_ReturnsHelloWorld()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/ping");

        var stringContent = await response.Content.ReadAsStringAsync();

        await Assert.That(stringContent).IsEqualTo("Hello, World!");
    }
}
