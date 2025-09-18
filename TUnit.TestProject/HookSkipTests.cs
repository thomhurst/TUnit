using TUnit.Core;

namespace TUnit.TestProject;

public class HookSkipTests
{
    public static string HookExecutionLog = "";

    [Before(Test)]
    [Skip("This before hook should be skipped")]
    public void SkippedBeforeHook()
    {
        HookExecutionLog += "BeforeHookExecuted";
    }

    [Before(Test)]
    public void ExecutedBeforeHook()
    {
        HookExecutionLog += "BeforeHookNotSkipped";
    }

    [After(Test)]
    [Skip("This after hook should be skipped")]
    public void SkippedAfterHook()
    {
        HookExecutionLog += "AfterHookExecuted";
    }

    [After(Test)]
    public void ExecutedAfterHook()
    {
        HookExecutionLog += "AfterHookNotSkipped";
    }

    [Test]
    public void TestWithSkippedHooks()
    {
        // Reset log before test
        HookExecutionLog = "";
        
        // This test should run normally
        // Only the hooks without Skip attributes should execute
    }
}

public class CustomSkipHook : SkipAttribute
{
    private readonly bool _shouldSkip;

    public CustomSkipHook(bool shouldSkip, string reason) : base(reason)
    {
        _shouldSkip = shouldSkip;
    }

    public override Task<bool> ShouldSkip(HookRegisteredContext context)
    {
        return Task.FromResult(_shouldSkip);
    }
}

public class ConditionalHookSkipTests
{
    public static string ConditionalHookLog = "";

    [Before(Test)]
    [CustomSkipHook(true, "This hook is conditionally skipped")]
    public void ConditionallySkippedHook()
    {
        ConditionalHookLog += "ConditionalSkipped";
    }

    [Before(Test)]
    [CustomSkipHook(false, "This hook should not be skipped")]
    public void ConditionallyNotSkippedHook()
    {
        ConditionalHookLog += "ConditionalNotSkipped";
    }

    [Test]
    public void TestWithConditionalSkippedHooks()
    {
        // Reset log before test
        ConditionalHookLog = "";
        
        // This test should run normally with selective hook execution
    }
}