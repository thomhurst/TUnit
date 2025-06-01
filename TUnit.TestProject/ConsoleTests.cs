using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class ConsoleTests
{
    [Test]
    public async Task Write_Source_Gen_Information()
    {
        Console.WriteLine(TestContext.Current!.TestDetails.TestMethod);
        await Assert.That(TestContext.Current.GetStandardOutput()).IsEqualTo(TestContext.Current.TestDetails.TestMethod.ToString());
    }
}