using TUnit.Core;

namespace TUnit.TestProject;

public class ComprehensiveHookSkipTests
{
    public static List<string> ExecutionLog = [];

    [BeforeEvery(Class)]
    [Skip("Skip before every class hook")]
    public static void SkippedBeforeEveryClassHook()
    {
        ExecutionLog.Add("SkippedBeforeEveryClassHook");
    }

    [BeforeEvery(Class)]
    public static void ExecutedBeforeEveryClassHook()
    {
        ExecutionLog.Add("ExecutedBeforeEveryClassHook");
    }

    [Before(Class)]
    [Skip("Skip before class hook")]
    public static void SkippedBeforeClassHook()
    {
        ExecutionLog.Add("SkippedBeforeClassHook");
    }

    [Before(Class)]
    public static void ExecutedBeforeClassHook()
    {
        ExecutionLog.Add("ExecutedBeforeClassHook");
    }

    [Before(Test)]
    [Skip("Skip before test hook")]
    public void SkippedBeforeTestHook()
    {
        ExecutionLog.Add("SkippedBeforeTestHook");
    }

    [Before(Test)]
    public void ExecutedBeforeTestHook()
    {
        ExecutionLog.Add("ExecutedBeforeTestHook");
    }

    [After(Test)]
    [Skip("Skip after test hook")]
    public void SkippedAfterTestHook()
    {
        ExecutionLog.Add("SkippedAfterTestHook");
    }

    [After(Test)]
    public void ExecutedAfterTestHook()
    {
        ExecutionLog.Add("ExecutedAfterTestHook");
    }

    [After(Class)]
    [Skip("Skip after class hook")]
    public static void SkippedAfterClassHook()
    {
        ExecutionLog.Add("SkippedAfterClassHook");
    }

    [After(Class)]
    public static void ExecutedAfterClassHook()
    {
        ExecutionLog.Add("ExecutedAfterClassHook");
    }

    [AfterEvery(Class)]
    [Skip("Skip after every class hook")]
    public static void SkippedAfterEveryClassHook()
    {
        ExecutionLog.Add("SkippedAfterEveryClassHook");
    }

    [AfterEvery(Class)]
    public static void ExecutedAfterEveryClassHook()
    {
        ExecutionLog.Add("ExecutedAfterEveryClassHook");
    }

    [Test]
    public void TestWithMultipleHooks()
    {
        // This test should run normally
        // Only the hooks without Skip attributes should execute
        ExecutionLog.Add("TestExecuted");
    }
}

// Test for custom skip logic
public class CustomConditionalSkip : SkipAttribute
{
    private readonly bool _shouldSkip;

    public CustomConditionalSkip(bool shouldSkip, string reason) : base(reason)
    {
        _shouldSkip = shouldSkip;
    }

    public override Task<bool> ShouldSkipHook(HookRegisteredContext context) => Task.FromResult(_shouldSkip);
    
    public override Task<bool> ShouldSkip(TestRegisteredContext context) => Task.FromResult(_shouldSkip);
}

public class CustomSkipLogicTests
{
    public static List<string> CustomSkipLog = [];

    [Before(Test)]
    [CustomConditionalSkip(true, "Should skip this hook")]
    public void ConditionallySkippedHook()
    {
        CustomSkipLog.Add("ConditionallySkippedHook");
    }

    [Before(Test)]
    [CustomConditionalSkip(false, "Should not skip this hook")]
    public void ConditionallyNotSkippedHook()
    {
        CustomSkipLog.Add("ConditionallyNotSkippedHook");
    }

    [Test]
    public void TestWithCustomSkipLogic()
    {
        CustomSkipLog.Add("TestWithCustomSkipLogic");
    }
}