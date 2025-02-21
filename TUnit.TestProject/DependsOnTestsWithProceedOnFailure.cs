namespace TUnit.TestProject;

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