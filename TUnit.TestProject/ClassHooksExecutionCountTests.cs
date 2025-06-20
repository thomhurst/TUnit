using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class ClassHooksExecutionCountTests
{
    private static int _beforeClassCalls;

    [Before(Class)]
    public static void BeforeClass()
    {
        _beforeClassCalls++;
    }

    [After(Class)]
    public static async Task AfterClass()
    {
        await Assert.That(_beforeClassCalls).IsEqualTo(1);
    }

    [Test]
    public void Test1()
    {
    }

    [Test]
    public void Test2()
    {
    }

    [Test]
    public void Test3()
    {
    }

    [Test]
    public void Test4()
    {
    }

    [Test]
    public void Test5()
    {
    }
}