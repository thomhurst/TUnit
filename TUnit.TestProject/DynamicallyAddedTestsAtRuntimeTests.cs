#pragma warning disable
namespace TUnit.TestProject;

public class DynamicallyAddedTestsAtRuntimeTests
{
    private static int _testRepeatLimit = 0;
    
    [Test]
    [Arguments(1)]
    public void Failure(int i)
    {
        throw new Exception($"Random reason: {i}");
    }

    [After(Test)]
    public void CreateRepeatTestIfFailure(TestContext context)
    {
        // TODO
    }
}