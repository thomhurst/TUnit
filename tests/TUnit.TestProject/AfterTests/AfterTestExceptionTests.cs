namespace TUnit.TestProject.AfterTests;

public class AfterTestExceptionTests
{
    private static bool _testExecuted;
    private static bool _afterHookExecuted;
    
    [Before(Class)]
    public static void ResetFlags()
    {
        _testExecuted = false;
        _afterHookExecuted = false;
    }
    
    [Test]
    public async Task Test_Should_Fail_When_After_Hook_Throws()
    {
        _testExecuted = true;
        await Task.CompletedTask;
        // Test itself passes
    }
    
    [After(Test)]
    public async Task AfterHook_That_Throws_Exception()
    {
        _afterHookExecuted = true;
        await Task.CompletedTask;
        throw new InvalidOperationException("After test hook intentionally failed!");
    }
    
    [After(Class)]
    public static void VerifyExecutions()
    {
        // Verify that both the test and after hook executed
        if (!_testExecuted)
        {
            throw new Exception("Test was not executed!");
        }
        
        if (!_afterHookExecuted)
        {
            throw new Exception("After hook was not executed!");
        }
    }
}

public class MultipleAfterHookExceptionTests
{
    [Test]
    public async Task Test_Should_Fail_When_Multiple_After_Hooks_Throw()
    {
        await Task.CompletedTask;
        // Test itself passes
    }
    
    [After(Test)]
    public async Task FirstAfterHook_That_Throws()
    {
        await Task.CompletedTask;
        throw new InvalidOperationException("First after hook failed!");
    }
    
    [After(Test)]
    public async Task SecondAfterHook_That_Throws()
    {
        await Task.CompletedTask;
        throw new InvalidOperationException("Second after hook failed!");
    }
}

public class TestAndAfterHookBothFailTests
{
    [Test]
    public async Task Test_And_After_Hook_Both_Fail()
    {
        await Task.CompletedTask;
        throw new InvalidOperationException("Test intentionally failed!");
    }
    
    [After(Test)]
    public async Task AfterHook_Also_Fails()
    {
        await Task.CompletedTask;
        throw new InvalidOperationException("After hook also failed!");
    }
}

public class PassingTestWithFailingAfterHook
{
    [Test]
    public void Simple_Passing_Test_With_Failing_After_Hook()
    {
        // Test passes
        Console.WriteLine("Test executed and passed");
    }
    
    [After(Test)]
    public void After_Hook_That_Fails()
    {
        Console.WriteLine("After hook executing...");
        throw new Exception("After hook failure should fail the test!");
    }
}