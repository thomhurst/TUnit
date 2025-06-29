using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Failure)]
public class DependsOnTestsWithProceedOnFailure
{
    [Test]
    public void Test1()
    {
        throw new Exception();
    }

    [Test, DependsOn(nameof(Test1), ProceedOnFailure = true)]
    public void Test2()
    {
    }
}
