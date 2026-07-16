using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Failure)]
public class FailedInitializationTests
{
    [Test]
    [MethodDataSource(nameof(Data))]
    public void FailingDataSource(int value)
    {
    }

    public static int Data() => throw new Exception("Initialization failed");
}
