using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.Example.Asp.Net.TestProject;

[ClassDataSource<WebApplicationFactory>(Shared = SharedType.None)]
[ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerClass)]
[ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerAssembly)]
[ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
[ClassDataSource<WebApplicationFactory>(Shared = SharedType.Keyed, Key = "🔑")]
public class Tests(WebApplicationFactory webApplicationFactory)
{
    [Repeat(5)]
    [Test]
    public async Task Test()
    {
        var client = webApplicationFactory.CreateClient();

        var response = await client.GetAsync("/ping");

        var stringContent = await response.Content.ReadAsStringAsync();

        await Assert.That(stringContent).IsEqualTo("Hello, World!");
        await Assert.That(webApplicationFactory.ConfiguredWebHostCalled).IsEqualTo(1);
    }
    
    

}