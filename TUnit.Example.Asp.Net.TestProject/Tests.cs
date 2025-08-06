namespace TUnit.Example.Asp.Net.TestProject;

public class Tests : TestsBase
{
    [Test]
    public async Task Test()
    {
        var client = WebApplicationFactory.CreateClient();

        var response = await client.GetAsync("/ping");

        var stringContent = await response.Content.ReadAsStringAsync();

        await Assert.That(stringContent).IsEqualTo("Hello, World!");
    }
}
