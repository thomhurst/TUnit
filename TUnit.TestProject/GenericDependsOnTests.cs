using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

// Test class with test methods to depend on
public class GenericDependsOnTestsClassA
{
    internal static DateTime Test1Start;

    [Test]
    public async Task Test1()
    {
        Test1Start = TestContext.Current!.Execution.TestStart!.Value.DateTime;
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
}

// Test for generic DependsOn with method name: DependsOn<T>(methodName)
[EngineTest(ExpectedResult.Pass)]
public class GenericDependsOnTestsWithClass
{
    private static DateTime _test2Start;

    [Test, DependsOn<GenericDependsOnTestsClassA>(nameof(GenericDependsOnTestsClassA.Test1))]
    public async Task Test2()
    {
        _test2Start = TestContext.Current!.Execution.TestStart!.Value.DateTime;
        await Task.CompletedTask;
    }

    [After(Class)]
    public static async Task AssertStartTimes()
    {
        await Assert.That(_test2Start).IsAfterOrEqualTo(GenericDependsOnTestsClassA.Test1Start.AddSeconds(4.9));
    }
}

// Test class with test methods to depend on for the second test
public class GenericDependsOnTestsClassB
{
    internal static DateTime Test1Start;

    [Test]
    public async Task Test1()
    {
        Test1Start = TestContext.Current!.Execution.TestStart!.Value.DateTime;
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
}

// Test for generic DependsOn without method name: DependsOn<T>()
// This should depend on all tests in ClassB
[EngineTest(ExpectedResult.Pass)]
public class GenericDependsOnTestsWithClassNoMethod
{
    private static DateTime _test2Start;

    [Test, DependsOn<GenericDependsOnTestsClassB>]
    public async Task Test2()
    {
        _test2Start = TestContext.Current!.Execution.TestStart!.Value.DateTime;
        await Task.CompletedTask;
    }

    [After(Class)]
    public static async Task AssertStartTimes()
    {
        await Assert.That(_test2Start).IsAfterOrEqualTo(GenericDependsOnTestsClassB.Test1Start.AddSeconds(4.9));
    }
}
