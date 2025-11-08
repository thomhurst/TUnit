using TUnit.Core;

namespace TUnit.TestProject;

public class SkipInBeforeClassHookTests
{
    [Before(Class)]
    public static void BeforeClass_Skip()
    {
        Skip.Test("Skipping from BeforeClass hook");
    }

    [Test]
    public void Test_ShouldBeSkipped()
    {
        // This test should never execute
    }
}

public class SkipInBeforeTestHookTests
{
    [Before(Test)]
    public void BeforeTest_Skip()
    {
        Skip.Test("Skipping from Before(Test) hook");
    }

    [Test]
    public void Test1_ShouldBeSkipped()
    {
        // This test should never execute
    }

    [Test]
    public void Test2_ShouldBeSkipped()
    {
        // This test should never execute
    }
}

