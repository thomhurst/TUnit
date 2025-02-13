namespace TUnit.TestProject;

public class DependsOnTestsWithoutProceedOnFailure
{
    [Test]
    public void Test1()
    {
        throw new Exception();
    }

    [Test, DependsOn(nameof(Test1))]
    public void Test2()
    {
    }
}