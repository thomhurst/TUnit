using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

/// <summary>
/// Stress tests for ConstraintKeyScheduler with overlapping constraint keys.
/// These tests create high contention scenarios to verify the scheduler
/// handles concurrent access to shared constraint keys without deadlocks.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class ConstraintKeyStressTests
{
    private static readonly ConcurrentBag<(string TestName, DateTimeOffset Start, DateTimeOffset End)> _executionTimes = [];
    private static int _completedTests = 0;

    [After(Test)]
    public async Task RecordExecution()
    {
        var context = TestContext.Current!;
        _executionTimes.Add((context.Metadata.TestDetails.TestName,
                            context.Execution.TestStart!.Value,
                            context.Execution.Result!.End!.Value));
        Interlocked.Increment(ref _completedTests);
        await Task.CompletedTask;
    }

    [After(Class)]
    public static async Task VerifyAllTestsCompleted()
    {
        await Task.Delay(100); // Ensure all tests recorded

        var times = _executionTimes.ToArray();

        // Check we have all 30 tests (10 methods × 3 runs each with Repeat(2))
        await Assert.That(times.Length).IsEqualTo(30);
        await Assert.That(_completedTests).IsEqualTo(30);
    }

    // Tests with constraint key "A" - will contend with each other
    [Test, Repeat(2)]
    [NotInParallel("A")]
    public async Task StressTest_KeyA_1()
    {
        await Task.Delay(50);
    }

    [Test, Repeat(2)]
    [NotInParallel("A")]
    public async Task StressTest_KeyA_2()
    {
        await Task.Delay(50);
    }

    // Tests with constraint key "B" - will contend with each other
    [Test, Repeat(2)]
    [NotInParallel("B")]
    public async Task StressTest_KeyB_1()
    {
        await Task.Delay(50);
    }

    [Test, Repeat(2)]
    [NotInParallel("B")]
    public async Task StressTest_KeyB_2()
    {
        await Task.Delay(50);
    }

    // Tests with overlapping constraint keys "A" and "B"
    // These create complex contention scenarios
    [Test, Repeat(2)]
    [NotInParallel(new[] { "A", "B" })]
    public async Task StressTest_KeyAB_1()
    {
        await Task.Delay(50);
    }

    [Test, Repeat(2)]
    [NotInParallel(new[] { "A", "B" })]
    public async Task StressTest_KeyAB_2()
    {
        await Task.Delay(50);
    }

    // Tests with constraint key "C" - independent of A and B
    [Test, Repeat(2)]
    [NotInParallel("C")]
    public async Task StressTest_KeyC_1()
    {
        await Task.Delay(50);
    }

    [Test, Repeat(2)]
    [NotInParallel("C")]
    public async Task StressTest_KeyC_2()
    {
        await Task.Delay(50);
    }

    // Tests with overlapping constraint keys "B" and "C"
    [Test, Repeat(2)]
    [NotInParallel(new[] { "B", "C" })]
    public async Task StressTest_KeyBC_1()
    {
        await Task.Delay(50);
    }

    [Test, Repeat(2)]
    [NotInParallel(new[] { "B", "C" })]
    public async Task StressTest_KeyBC_2()
    {
        await Task.Delay(50);
    }
}
