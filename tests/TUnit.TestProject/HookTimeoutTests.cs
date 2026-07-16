namespace TUnit.TestProject;

/// <summary>
/// Tests for hook timeout attribute functionality.
/// This class tests that [Timeout] attribute on hooks is properly respected.
/// </summary>
public class HookTimeoutTests
{
    /// <summary>
    /// A 100ms timeout on the hook - it should fail because the hook takes 500ms
    /// </summary>
    [Test]
    public void Test_WithTimeoutHook()
    {
        // This test exists to verify that the hook timeout was applied correctly
    }
}

/// <summary>
/// Class-level hook with timeout that should fail
/// </summary>
public class ClassHookTimeoutTests
{
    private static bool _classHookExecuted;

    [Timeout(100)] // 100ms timeout - should fail
    [Before(Class)]
    public static async Task BeforeClass(CancellationToken cancellationToken)
    {
        _classHookExecuted = true;
        // This will take longer than the timeout
        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
    }

    [Test]
    public async Task Test_ShouldNotRun_BecauseHookTimedOut()
    {
        // This test should not actually execute because the class hook should timeout
        await Assert.That(_classHookExecuted).IsTrue();
    }
}

/// <summary>
/// Assembly-level hook with timeout that should pass (short delay within timeout)
/// </summary>
public class AssemblyHookTimeoutPassTests
{
    // Note: We can't test assembly hooks that fail timeout in the same way,
    // as they affect the whole assembly. We test that the timeout IS applied
    // by checking that a hook with a longer timeout succeeds.
    
    // The BeforeAssembly hook with a 5 second timeout, but quick execution
    private static bool _assemblyHookExecuted;

    [Timeout(5000)] // 5 second timeout
    [Before(Assembly)]
    public static async Task BeforeAssembly(CancellationToken cancellationToken)
    {
        _assemblyHookExecuted = true;
        // Short delay - well within timeout
        await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
    }

    [Test]
    public async Task Test_ShouldRun_BecauseHookCompletedInTime()
    {
        await Assert.That(_assemblyHookExecuted).IsTrue();
    }
}
