using TUnit.Core;

namespace TUnit.TestProject;

public class CustomSkipWithHooksTestAttribute(string reason) : SkipAttribute(reason)
{
    public override Task<bool> ShouldSkip(TestRegisteredContext context)
    {
        return Task.FromResult(true);
    }
}

public class SkipHookExecutionTest
{
    public static readonly List<string> ExecutedHooks = [];

    [Test]
    [Skip("Direct skip attribute")]
    public void TestWithDirectSkipAttribute()
    {
        // This should never execute
        ExecutedHooks.Add("TestWithDirectSkipAttribute executed");
    }

    [Test]
    [CustomSkipWithHooksTest("Custom skip attribute")]
    public void TestWithCustomSkipAttribute()
    {
        // This should never execute
        ExecutedHooks.Add("TestWithCustomSkipAttribute executed");
    }

    [Test]
    public void TestExecutionValidation()
    {
        // This test validates that hooks were not executed for skipped tests
        // For direct skip: no hooks should run
        // For custom skip: hooks should not run either (after fix)
        
        var directSkipHooks = ExecutedHooks.Where(h => h.Contains("TestWithDirectSkipAttribute")).ToList();
        var customSkipHooks = ExecutedHooks.Where(h => h.Contains("TestWithCustomSkipAttribute")).ToList();
        
        Console.WriteLine($"Direct skip hooks executed: {string.Join(", ", directSkipHooks)}");
        Console.WriteLine($"Custom skip hooks executed: {string.Join(", ", customSkipHooks)}");
        
        // Both should be empty after the fix
        if (directSkipHooks.Count > 0 || customSkipHooks.Count > 0)
        {
            throw new Exception($"Hooks should not execute for skipped tests. Direct: {directSkipHooks.Count}, Custom: {customSkipHooks.Count}");
        }
    }

    // Instance hooks to test the InstanceHookMethod fix
    [Before(Test)]
    public void BeforeTest(TestContext testContext)
    {
        ExecutedHooks.Add($"Before: {testContext.TestDetails.TestName}");
        Console.WriteLine($"Before executed for: {testContext.TestDetails.TestName}");
    }

    [After(Test)]
    public void AfterTest(TestContext testContext)
    {
        ExecutedHooks.Add($"After: {testContext.TestDetails.TestName}");
        Console.WriteLine($"After executed for: {testContext.TestDetails.TestName}");
    }
}

public static class SkipHookExecutionTestHooks
{
    [BeforeEvery(Test)]
    public static void BeforeEveryTest(TestContext testContext)
    {
        SkipHookExecutionTest.ExecutedHooks.Add($"BeforeEvery: {testContext.TestDetails.TestName}");
        Console.WriteLine($"BeforeEvery executed for: {testContext.TestDetails.TestName}");
    }

    [AfterEvery(Test)]
    public static void AfterEveryTest(TestContext testContext)
    {
        SkipHookExecutionTest.ExecutedHooks.Add($"AfterEvery: {testContext.TestDetails.TestName}");
        Console.WriteLine($"AfterEvery executed for: {testContext.TestDetails.TestName}");
    }
}