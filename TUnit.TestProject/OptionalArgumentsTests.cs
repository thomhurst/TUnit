using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class OptionalArgumentsTests
{
    [Test]
    [Arguments(1)]
    public void Test(int value, bool flag = true)
    {
        // Dummy Method
    }
}