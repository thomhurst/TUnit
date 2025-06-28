using TUnit.Core;

namespace TUnit.TestProject;

[Property("EngineTest", "Pass")]
public class SimplePropertyFilterTest
{
    [Test]
    public void TestWithPropertyAttribute()
    {
        Console.WriteLine($"Test executed! Properties: {string.Join(", ", TestContext.Current?.TestDetails.CustomProperties.Select(kvp => $"{kvp.Key}={string.Join(",", kvp.Value)}") ?? [])}");
    }
}