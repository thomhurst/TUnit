using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Failure)]
public class DependsOnTestsWithoutProceedOnFailure
{
    [Test]
    public void Test1()
    {
        throw new Exception("This exception should block Test2 from executing");
    }

    [Test, DependsOn(nameof(Test1))]
    public void Test2()
    {
    }
}
