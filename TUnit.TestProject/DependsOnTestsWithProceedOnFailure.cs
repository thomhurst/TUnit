using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Failure)]
public class DependsOnTestsWithProceedOnFailure
{
    [Test]
    public void Test1()
    {
        throw new Exception("This failure should still allow Test2 to execute");
    }

    [Test, DependsOn(nameof(Test1), ProceedOnFailure = true)]
    public void Test2()
    {
    }
}
