using TUnit.Core;

namespace TUnit.Engine;

internal class TestsLoader
{
    public IEnumerable<TestDetails> GetTests()
    {
        // TODO: Can we improve on this?
        return TestDictionary.GetAllTestDetails()
            .Select(x => new TestDetails(
                x.TestId,
                x.MethodInfo, 
                x.ClassType,
                x.TestMethodArguments,
                x.TestClassArguments,
                x.ClassRepeatCount,
                x.MethodRepeatCount));
    }
}