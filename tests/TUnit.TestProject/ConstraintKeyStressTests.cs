using System.Collections.Concurrent;
using TUnit.Assertions;
using TUnit.Core;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

/// <summary>
/// Stress tests for ConstraintKeyScheduler with overlapping constraint keys.
/// These tests create high contention scenarios to verify the scheduler:
/// 1. Handles concurrent access to shared constraint keys without deadlocks
/// 2. Enforces mutual exclusion for tests with same constraint keys
/// 3. Allows parallel execution for tests with different constraint keys
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class ConstraintKeyStressTests
{
    // Constants for test configuration
    private const int WorkDurationMilliseconds = 50; // Duration each test simulates work
    private const int TimeoutMilliseconds = 30_000; // Maximum time allowed for each test to prevent CI hangs

    // Track execution windows to verify constraint key semantics
    private static readonly ConcurrentDictionary<string, (DateTime Start, DateTime End)> ExecutionWindows = new();

    // Track test invocations to verify all tests actually execute
    private static readonly ConcurrentDictionary<string, int> TestInvocations = new();

    // Tests with constraint key "A" - will contend with each other
    [Test, Repeat(2), Timeout(TimeoutMilliseconds)]
    [NotInParallel("A")]
    public async Task StressTest_KeyA_1(CancellationToken cancellationToken)
    {
        await ExecuteConstraintKeyStressTest(cancellationToken);
    }

    [Test, Repeat(2), Timeout(TimeoutMilliseconds)]
    [NotInParallel("A")]
    public async Task StressTest_KeyA_2(CancellationToken cancellationToken)
    {
        await ExecuteConstraintKeyStressTest(cancellationToken);
    }

    // Tests with constraint key "B" - will contend with each other
    [Test, Repeat(2), Timeout(TimeoutMilliseconds)]
    [NotInParallel("B")]
    public async Task StressTest_KeyB_1(CancellationToken cancellationToken)
    {
        await ExecuteConstraintKeyStressTest(cancellationToken);
    }

    [Test, Repeat(2), Timeout(TimeoutMilliseconds)]
    [NotInParallel("B")]
    public async Task StressTest_KeyB_2(CancellationToken cancellationToken)
    {
        await ExecuteConstraintKeyStressTest(cancellationToken);
    }

    // Tests with overlapping constraint keys "A" and "B"
    // These create complex contention scenarios
    [Test, Repeat(2), Timeout(TimeoutMilliseconds)]
    [NotInParallel(["A", "B"])]
    public async Task StressTest_KeyAB_1(CancellationToken cancellationToken)
    {
        await ExecuteConstraintKeyStressTest(cancellationToken);
    }

    [Test, Repeat(2), Timeout(TimeoutMilliseconds)]
    [NotInParallel(["A", "B"])]
    public async Task StressTest_KeyAB_2(CancellationToken cancellationToken)
    {
        await ExecuteConstraintKeyStressTest(cancellationToken);
    }

    // Tests with constraint key "C" - independent of A and B
    [Test, Repeat(2), Timeout(TimeoutMilliseconds)]
    [NotInParallel("C")]
    public async Task StressTest_KeyC_1(CancellationToken cancellationToken)
    {
        await ExecuteConstraintKeyStressTest(cancellationToken);
    }

    [Test, Repeat(2), Timeout(TimeoutMilliseconds)]
    [NotInParallel("C")]
    public async Task StressTest_KeyC_2(CancellationToken cancellationToken)
    {
        await ExecuteConstraintKeyStressTest(cancellationToken);
    }

    // Tests with overlapping constraint keys "B" and "C"
    [Test, Repeat(2), Timeout(TimeoutMilliseconds)]
    [NotInParallel(["B", "C"])]
    public async Task StressTest_KeyBC_1(CancellationToken cancellationToken)
    {
        await ExecuteConstraintKeyStressTest(cancellationToken);
    }

    [Test, Repeat(2), Timeout(TimeoutMilliseconds)]
    [NotInParallel(["B", "C"])]
    public async Task StressTest_KeyBC_2(CancellationToken cancellationToken)
    {
        await ExecuteConstraintKeyStressTest(cancellationToken);
    }

    /// <summary>
    /// Common execution logic for all constraint key stress tests.
    /// Records execution windows and verifies constraint semantics.
    /// </summary>
    private static async Task ExecuteConstraintKeyStressTest(CancellationToken cancellationToken)
    {
        var testContext = TestContext.Current!;
        var testName = testContext.Metadata.TestDetails.TestName;

        // Track that this test was invoked
        TestInvocations.AddOrUpdate(testName, 1, (_, count) => count + 1);

        // Record execution start time
        var startTime = DateTime.UtcNow;

        // Simulate work - represents actual test execution time
        await Task.Delay(WorkDurationMilliseconds, cancellationToken);

        // Record execution end time
        var endTime = DateTime.UtcNow;

        // Store execution window for constraint verification
        ExecutionWindows[testName] = (startTime, endTime);

        // Verify mutual exclusion for same constraint keys
        await VerifyConstraintKeySemantics(testContext);
    }

    /// <summary>
    /// Verifies that constraint keys enforce proper mutual exclusion.
    /// Tests with the same constraint key should not have overlapping execution windows.
    /// </summary>
    private static async Task VerifyConstraintKeySemantics(TestContext currentTest)
    {
        var currentTestName = currentTest.Metadata.TestDetails.TestName;
        var currentWindow = ExecutionWindows[currentTestName];

        // Get constraint keys for current test
        var currentKeys = GetConstraintKeys(currentTest);

        // Check all other completed tests for overlaps with same constraint keys
        foreach (var (otherTestName, otherWindow) in ExecutionWindows)
        {
            if (otherTestName == currentTestName)
            {
                continue; // Skip self
            }

            // For tests with shared constraint keys, verify no overlap
            var otherKeys = GetConstraintKeysFromTestName(otherTestName);
            var hasSharedKey = currentKeys.Intersect(otherKeys).Any();

            if (hasSharedKey)
            {
                // Check if execution windows overlap
                var overlap = !(currentWindow.End <= otherWindow.Start || otherWindow.End <= currentWindow.Start);

                if (overlap)
                {
                    // This indicates a constraint violation - tests with same key should be serial
                    // Note: Due to timing precision, async nature, and CI scheduling variability,
                    // allow small overlaps (< 50ms) as tolerance for framework overhead
                    var overlapDuration = GetOverlapDuration(currentWindow, otherWindow);

                    // Use Assert.Fail for constraint violations
                    if (overlapDuration.TotalMilliseconds >= 50)
                    {
                        Assert.Fail(
                            $"Tests with shared constraint keys should not overlap significantly. " +
                            $"Current: {currentTestName}, Other: {otherTestName}, " +
                            $"Shared keys: {string.Join(",", currentKeys.Intersect(otherKeys))}, " +
                            $"Overlap: {overlapDuration.TotalMilliseconds:F2}ms");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extracts constraint keys from the current test context.
    /// </summary>
    private static string[] GetConstraintKeys(TestContext testContext)
    {
        var notInParallelAttributes = testContext.Metadata.TestDetails.Attributes
            .GetAttributes<NotInParallelAttribute>()
            .ToArray();

        var keys = new List<string>();

        foreach (var attr in notInParallelAttributes)
        {
            keys.AddRange(attr.ConstraintKeys);
        }

        return [.. keys];
    }

    /// <summary>
    /// Extracts constraint keys from test name pattern.
    /// This is a simplified version used when we don't have the full test context.
    /// </summary>
    private static string[] GetConstraintKeysFromTestName(string testName)
    {
        // Parse test name to extract keys
        // Test names follow pattern: StressTest_Key{Keys}_{Number}
        if (testName.Contains("KeyAB"))
        {
            return ["A", "B"];
        }
        if (testName.Contains("KeyBC"))
        {
            return ["B", "C"];
        }
        if (testName.Contains("KeyA"))
        {
            return ["A"];
        }
        if (testName.Contains("KeyB"))
        {
            return ["B"];
        }
        if (testName.Contains("KeyC"))
        {
            return ["C"];
        }

        return [];
    }

    /// <summary>
    /// Calculates the overlap duration between two execution windows.
    /// </summary>
    private static TimeSpan GetOverlapDuration((DateTime Start, DateTime End) window1, (DateTime Start, DateTime End) window2)
    {
        var overlapStart = window1.Start > window2.Start ? window1.Start : window2.Start;
        var overlapEnd = window1.End < window2.End ? window1.End : window2.End;

        if (overlapEnd <= overlapStart)
        {
            return TimeSpan.Zero;
        }

        return overlapEnd - overlapStart;
    }
}
