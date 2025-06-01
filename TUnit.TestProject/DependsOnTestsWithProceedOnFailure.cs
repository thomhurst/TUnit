using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public class DependsOnTestsWithProceedOnFailure
{
    [Test]
    [EngineTest(ExpectedResult.Failure)]
    public void Test1()
    {
        throw new Exception();
    }

    [EngineTest(ExpectedResult.Pass)]
    [Test, DependsOn(nameof(Test1), ProceedOnFailure = true)]
    public void Test2()
    {
    }
}