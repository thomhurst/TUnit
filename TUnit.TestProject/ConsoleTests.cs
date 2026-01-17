using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class ConsoleTests
{
    [Test]
    public async Task Write_Source_Gen_Information()
    {
        Console.WriteLine(TestContext.Current!.Metadata.TestDetails.MethodMetadata);
        await Assert.That(TestContext.Current.GetStandardOutput()).Contains(TestContext.Current.Metadata.TestDetails.MethodMetadata.ToString()!);
    }

    [Test]
    [Explicit]
    public async Task StreamsToIde()
    {
        for (var i = 0; i < 10; i++)
        {
            Console.WriteLine(@$"{i}...");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}
