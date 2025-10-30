using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class ConsoleTests
{
    [Test]
    public async Task Write_Source_Gen_Information()
    {
        Console.WriteLine(TestContext.Current!.Metadata.TestDetails.MethodMetadata);
        await Assert.That(TestContext.Current.GetStandardOutput()).IsEqualTo(TestContext.Current.Metadata.TestDetails.MethodMetadata.ToString());
    }
}
