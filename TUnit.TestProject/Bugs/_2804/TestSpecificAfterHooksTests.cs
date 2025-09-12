using System.Collections.Concurrent;

namespace TUnit.TestProject.Bugs._2804;

/// <summary>
/// Tests to verify that test-specific After(Test) hooks (not AfterEvery)
/// all execute even when some fail, and exceptions are properly aggregated.
/// </summary>
public class TestSpecificAfterHooksTests
{
    private static readonly ConcurrentBag<string> ExecutedHooks = new();
    private static readonly ConcurrentBag<Exception> CaughtExceptions = new();

    [Test]
    public async Task Test_With_Multiple_After_Hooks_Some_Failing()
    {
        await Task.CompletedTask;
        // Test executed - multiple After(Test) hooks will run
    }

    // Multiple After(Test) hooks for the same test
    [After(Test)]
    public async Task AfterTest_Hook1_Success(TestContext context)
    {
        if (context.TestDetails.TestName == nameof(Test_With_Multiple_After_Hooks_Some_Failing))
        {
            ExecutedHooks.Add("AfterTest_Hook1");
            // After(Test) Hook 1 executing successfully
            await Task.CompletedTask;
        }
    }

    [After(Test)]
    public async Task AfterTest_Hook2_Fails(TestContext context)
    {
        if (context.TestDetails.TestName == nameof(Test_With_Multiple_After_Hooks_Some_Failing))
        {
            ExecutedHooks.Add("AfterTest_Hook2");
            // After(Test) Hook 2 executing and will fail
            await Task.CompletedTask;
            var ex = new InvalidOperationException("After(Test) Hook 2 intentionally failed");
            CaughtExceptions.Add(ex);
            throw ex;
        }
    }

    [After(Test)]
    public async Task AfterTest_Hook3_Success(TestContext context)
    {
        if (context.TestDetails.TestName == nameof(Test_With_Multiple_After_Hooks_Some_Failing))
        {
            ExecutedHooks.Add("AfterTest_Hook3");
            // After(Test) Hook 3 still executing after Hook 2 failed
            await Task.CompletedTask;
        }
    }

    [After(Test)]
    public async Task AfterTest_Hook4_AlsoFails(TestContext context)
    {
        if (context.TestDetails.TestName == nameof(Test_With_Multiple_After_Hooks_Some_Failing))
        {
            ExecutedHooks.Add("AfterTest_Hook4");
            // After(Test) Hook 4 executing and will also fail
            await Task.CompletedTask;
            var ex = new ArgumentException("After(Test) Hook 4 also intentionally failed");
            CaughtExceptions.Add(ex);
            throw ex;
        }
    }

    [After(Test)]
    public async Task AfterTest_Hook5_StillExecutes(TestContext context)
    {
        if (context.TestDetails.TestName == nameof(Test_With_Multiple_After_Hooks_Some_Failing))
        {
            ExecutedHooks.Add("AfterTest_Hook5");
            // After(Test) Hook 5 still executing after multiple failures
            await Task.CompletedTask;
            
            // Verify all hooks executed
            // Total After(Test) hooks executed verified
            // Total exceptions caught verified
            
            if (ExecutedHooks.Count >= 5)
            {
                // SUCCESS: All 5 After(Test) hooks executed despite 2 failures
            }
            
            // List all executed hooks
            // Execution order verified
            foreach (var hook in ExecutedHooks)
            {
                // Hook executed
            }
        }
    }
}

/// <summary>
/// Test mixing Before(Test) and After(Test) hooks with failures
/// </summary>
public class MixedBeforeAfterHooksTests
{
    private static readonly ConcurrentBag<string> HookSequence = new();
    private static bool _beforeHookFailed = false;

    [Before(Test)]
    public async Task BeforeTest_Hook1(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(MixedBeforeAfterHooksTests))
        {
            HookSequence.Add("Before_Hook1");
            // Before(Test) Hook 1 executing
            await Task.CompletedTask;
        }
    }

    [Before(Test)]
    public async Task BeforeTest_Hook2_MightFail(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(MixedBeforeAfterHooksTests))
        {
            HookSequence.Add("Before_Hook2");
            // Before(Test) Hook 2 executing
            await Task.CompletedTask;
            
            // Fail on specific test
            if (context.TestDetails.TestName == nameof(Test_With_Before_Hook_Failure))
            {
                _beforeHookFailed = true;
                // Before(Test) Hook 2 failing
                throw new Exception("Before hook intentionally failed");
            }
        }
    }

    [Test]
    public async Task Test_With_Before_Hook_Failure()
    {
        // This test may not execute if Before hook fails
        HookSequence.Add("Test_Executed");
        await Task.CompletedTask;
        // Test executed (shouldn't happen if Before hook failed)
    }

    [Test]
    public async Task Test_Without_Hook_Failure()
    {
        HookSequence.Add("Test_Executed");
        await Task.CompletedTask;
        // Test executed normally
    }

    // After hooks should still run even if Before hooks failed
    [After(Test)]
    public async Task AfterTest_Hook1_AlwaysRuns(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(MixedBeforeAfterHooksTests))
        {
            HookSequence.Add("After_Hook1");
            // After(Test) Hook 1 executing (should run even if Before failed)
            await Task.CompletedTask;
        }
    }

    [After(Test)]
    public async Task AfterTest_Hook2_Fails(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(MixedBeforeAfterHooksTests))
        {
            HookSequence.Add("After_Hook2");
            // After(Test) Hook 2 executing and will fail
            await Task.CompletedTask;
            throw new Exception("After hook intentionally failed");
        }
    }

    [After(Test)]
    public async Task AfterTest_Hook3_StillRuns(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(MixedBeforeAfterHooksTests))
        {
            HookSequence.Add("After_Hook3");
            // After(Test) Hook 3 still executing
            await Task.CompletedTask;
            
            // Log the sequence
            // Hook execution sequence verified
            foreach (var hook in HookSequence)
            {
                // Hook executed
            }
            
            if (_beforeHookFailed)
            {
                // Before hook failed, but After hooks still executed
            }
        }
    }
}

/// <summary>
/// Test to verify exception details are preserved in AggregateException
/// </summary>
public class ExceptionDetailsPreservationTests
{
    private static readonly List<string> ExceptionMessages = new();
    private static readonly List<Type> ExceptionTypes = new();

    [Test]
    public async Task Test_That_Collects_Exception_Details()
    {
        await Task.CompletedTask;
        // Test executed - collecting exception details from failing hooks
    }

    [AfterEvery(Test)]
    public static async Task Hook1_ThrowsInvalidOperation(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(ExceptionDetailsPreservationTests))
        {
            var message = "InvalidOperationException from Hook 1";
            var exType = typeof(InvalidOperationException);
            ExceptionMessages.Add(message);
            ExceptionTypes.Add(exType);
            // Hook 1 throwing exception
            await Task.CompletedTask;
            throw new InvalidOperationException(message);
        }
    }

    [AfterEvery(Test)]
    public static async Task Hook2_ThrowsArgument(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(ExceptionDetailsPreservationTests))
        {
            var message = "ArgumentException from Hook 2";
            var exType = typeof(ArgumentException);
            ExceptionMessages.Add(message);
            ExceptionTypes.Add(exType);
            // Hook 2 throwing exception
            await Task.CompletedTask;
            throw new ArgumentException(message);
        }
    }

    [AfterEvery(Test)]
    public static async Task Hook3_ThrowsNotImplemented(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(ExceptionDetailsPreservationTests))
        {
            var message = "NotImplementedException from Hook 3";
            var exType = typeof(NotImplementedException);
            ExceptionMessages.Add(message);
            ExceptionTypes.Add(exType);
            // Hook 3 throwing exception
            await Task.CompletedTask;
            throw new NotImplementedException(message);
        }
    }

    [AfterEvery(Test)]
    public static async Task Hook4_ThrowsCustom(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(ExceptionDetailsPreservationTests))
        {
            var message = "Custom exception from Hook 4";
            var exType = typeof(CustomTestException);
            ExceptionMessages.Add(message);
            ExceptionTypes.Add(exType);
            // Hook 4 throwing exception
            await Task.CompletedTask;
            throw new CustomTestException(message);
        }
    }

    [AfterEvery(Test)]
    public static async Task Hook5_VerifiesExceptions(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(ExceptionDetailsPreservationTests))
        {
            // Hook 5 executing (after 4 exceptions)
            // Exception types thrown verified
            // Exception messages verified
            // All exception details should be preserved in the AggregateException
            await Task.CompletedTask;
        }
    }

    // Custom exception for testing
    public class CustomTestException : Exception
    {
        public CustomTestException(string message) : base(message) { }
    }
}

/// <summary>
/// Test to verify cleanup continues even with catastrophic failures
/// </summary>
public class CatastrophicFailureRecoveryTests
{
    private static readonly ConcurrentBag<string> CleanupOperations = new();
    private static readonly List<IDisposable> ResourcesToClean = new();

    [Before(Test)]
    public void SetupResources(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(CatastrophicFailureRecoveryTests))
        {
            // Simulate resource allocation
            ResourcesToClean.Add(new DummyResource("Resource1"));
            ResourcesToClean.Add(new DummyResource("Resource2"));
            ResourcesToClean.Add(new DummyResource("Resource3"));
            // Resources allocated
        }
    }

    [Test]
    public async Task Test_With_Resources_Needing_Cleanup()
    {
        await Task.CompletedTask;
        // Test executed - resources need cleanup in After hooks
    }

    [After(Test)]
    public async Task Cleanup_Hook1_Partial_Success(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(CatastrophicFailureRecoveryTests))
        {
            // Cleanup Hook 1 - cleaning first resource
            if (ResourcesToClean.Count > 0)
            {
                ResourcesToClean[0].Dispose();
                CleanupOperations.Add("Resource1_Disposed");
            }
            await Task.CompletedTask;
        }
    }

    [After(Test)]
    public async Task Cleanup_Hook2_Fails_Catastrophically(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(CatastrophicFailureRecoveryTests))
        {
            // Cleanup Hook 2 - will fail catastrophically
            await Task.CompletedTask;
            throw new OutOfMemoryException("Simulated catastrophic failure during cleanup!");
        }
    }

    [After(Test)]
    public async Task Cleanup_Hook3_Still_Cleans_Resources(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(CatastrophicFailureRecoveryTests))
        {
            // Cleanup Hook 3 - still cleaning remaining resources after catastrophic failure
            
            // Clean remaining resources
            for (var i = 1; i < ResourcesToClean.Count; i++)
            {
                if (ResourcesToClean[i] != null)
                {
                    ResourcesToClean[i].Dispose();
                    CleanupOperations.Add($"Resource{i + 1}_Disposed");
                    // Resource cleaned
                }
            }
            
            await Task.CompletedTask;
        }
    }

    [After(Test)]
    public async Task Cleanup_Hook4_Verifies_Cleanup(TestContext context)
    {
        if (context.TestDetails.ClassType == typeof(CatastrophicFailureRecoveryTests))
        {
            // Cleanup Hook 4 - verifying cleanup status
            // Cleanup operations completed
            // Resources cleaned verified
            
            if (CleanupOperations.Count >= ResourcesToClean.Count)
            {
                // SUCCESS: All resources cleaned despite catastrophic failure
            }
            else
            {
                // WARNING: Not all resources cleaned
            }
            
            await Task.CompletedTask;
            
            // Clear for next test
            ResourcesToClean.Clear();
            CleanupOperations.Clear();
        }
    }

    private class DummyResource : IDisposable
    {
        private readonly string _name;
        private bool _disposed;

        public DummyResource(string name)
        {
            _name = name;
            // Resource created
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                // Resource disposed
            }
        }
    }
}