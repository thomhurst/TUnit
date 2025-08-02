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
        // Implementation pending - intended to demonstrate dynamic test repetition on failure
        // See DynamicallyRegisteredTests.cs for a working example using ReregisterTestWithArguments
        // Note: ReregisterTestWithArguments is currently marked as Obsolete and non-functional
        
        // Example implementation (currently non-functional):
        // if (context.Result?.State == TestState.Failed && _testRepeatLimit < 3)
        // {
        //     _testRepeatLimit++;
        //     await context.ReregisterTestWithArguments(methodArguments: [_testRepeatLimit]);
        // }
    }
}
