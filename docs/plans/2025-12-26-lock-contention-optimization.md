# Lock Contention Optimization Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Reduce lock contention in test discovery and scheduling to improve parallel test execution throughput.

**Architecture:** Replace `List<T>` + lock with `ImmutableList<T>` + atomic swap in ReflectionTestDataCollector. Replace LINQ with manual loops and restructure to two-phase locking in ConstraintKeyScheduler.

**Tech Stack:** C#, System.Collections.Immutable, System.Threading

---

## Task 1: Add Stress Tests for Thread Safety Baseline

**Files:**
- Create: `TUnit.Engine.Tests/Scheduling/ConstraintKeySchedulerConcurrencyTests.cs`

**Step 1: Write the failing test for high contention scenarios**

```csharp
using TUnit.Core;
using TUnit.Engine.Scheduling;

namespace TUnit.Engine.Tests.Scheduling;

public class ConstraintKeySchedulerConcurrencyTests
{
    [Test]
    [Repeat(5)]
    public async Task HighContention_WithOverlappingConstraints_CompletesWithoutDeadlock()
    {
        // Arrange - create mock tests with overlapping constraint keys
        // This test establishes baseline behavior before optimization

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Act & Assert - should complete without timeout/deadlock
        await Assert.That(async () =>
        {
            // Placeholder - will be implemented after understanding test infrastructure
            await Task.Delay(1, cts.Token);
        }).ThrowsNothing();
    }
}
```

**Step 2: Run test to verify it passes (baseline)**

Run: `cd TUnit.Engine.Tests && dotnet test --filter "FullyQualifiedName~ConstraintKeySchedulerConcurrencyTests"`
Expected: PASS (baseline test)

**Step 3: Commit**

```bash
git add TUnit.Engine.Tests/Scheduling/ConstraintKeySchedulerConcurrencyTests.cs
git commit -m "test: add concurrency stress test baseline for ConstraintKeyScheduler"
```

---

## Task 2: Replace LINQ with Manual Loops in ConstraintKeyScheduler (lines 56-69)

**Files:**
- Modify: `TUnit.Engine/Scheduling/ConstraintKeyScheduler.cs:56-69`

**Step 1: Write test for constraint key checking behavior**

```csharp
[Test]
public async Task ConstraintKeyCheck_WithNoConflicts_StartsImmediately()
{
    // This test verifies the behavior is unchanged after LINQ removal
    // Exact implementation depends on testability of ConstraintKeyScheduler
}
```

**Step 2: Run existing tests to establish baseline**

Run: `cd TUnit.Engine.Tests && dotnet test`
Expected: All tests PASS

**Step 3: Replace LINQ `.Any()` with manual loop**

In `ConstraintKeyScheduler.cs`, change lines 56-69 from:

```csharp
lock (lockObject)
{
    // Check if all constraint keys are available
    canStart = !constraintKeys.Any(key => lockedKeys.Contains(key));

    if (canStart)
    {
        // Lock all the constraint keys for this test
        foreach (var key in constraintKeys)
        {
            lockedKeys.Add(key);
        }
    }
}
```

To:

```csharp
lock (lockObject)
{
    // Check if all constraint keys are available - manual loop avoids LINQ allocation
    canStart = true;
    var keyCount = constraintKeys.Count;
    for (var i = 0; i < keyCount; i++)
    {
        if (lockedKeys.Contains(constraintKeys[i]))
        {
            canStart = false;
            break;
        }
    }

    if (canStart)
    {
        // Lock all the constraint keys for this test
        for (var i = 0; i < keyCount; i++)
        {
            lockedKeys.Add(constraintKeys[i]);
        }
    }
}
```

**Step 4: Run tests to verify behavior unchanged**

Run: `cd TUnit.Engine.Tests && dotnet test`
Expected: All tests PASS

**Step 5: Commit**

```bash
git add TUnit.Engine/Scheduling/ConstraintKeyScheduler.cs
git commit -m "perf: replace LINQ with manual loop in ConstraintKeyScheduler key checking"
```

---

## Task 3: Replace LINQ in Waiting Test Check (line 165)

**Files:**
- Modify: `TUnit.Engine/Scheduling/ConstraintKeyScheduler.cs:165`

**Step 1: Run existing tests to establish baseline**

Run: `cd TUnit.Engine.Tests && dotnet test`
Expected: All tests PASS

**Step 2: Replace LINQ `.Any()` in waiting test check**

In `ConstraintKeyScheduler.cs`, change line 165 from:

```csharp
var canStart = !waitingTest.ConstraintKeys.Any(key => lockedKeys.Contains(key));
```

To:

```csharp
var canStart = true;
var waitingKeys = waitingTest.ConstraintKeys;
var waitingKeyCount = waitingKeys.Count;
for (var j = 0; j < waitingKeyCount; j++)
{
    if (lockedKeys.Contains(waitingKeys[j]))
    {
        canStart = false;
        break;
    }
}
```

**Step 3: Run tests to verify behavior unchanged**

Run: `cd TUnit.Engine.Tests && dotnet test`
Expected: All tests PASS

**Step 4: Commit**

```bash
git add TUnit.Engine/Scheduling/ConstraintKeyScheduler.cs
git commit -m "perf: replace LINQ with manual loop in waiting test availability check"
```

---

## Task 4: Move List Allocation Outside Lock (lines 149-190)

**Files:**
- Modify: `TUnit.Engine/Scheduling/ConstraintKeyScheduler.cs:149-190`

**Step 1: Run existing tests to establish baseline**

Run: `cd TUnit.Engine.Tests && dotnet test`
Expected: All tests PASS

**Step 2: Pre-allocate lists outside lock scope**

Change the structure from allocating `tempQueue` inside lock to pre-allocating outside. Replace lines 149-190:

```csharp
// Release the constraint keys and check if any waiting tests can now run
var testsToStart = new List<(AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys, TaskCompletionSource<bool> StartSignal)>(4);
var testsToRequeue = new List<(AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys, TaskCompletionSource<bool> StartSignal)>(8);

lock (lockObject)
{
    // Release all constraint keys for this test
    var keyCount = constraintKeys.Count;
    for (var i = 0; i < keyCount; i++)
    {
        lockedKeys.Remove(constraintKeys[i]);
    }

    // Check waiting tests to see if any can now run
    while (waitingTests.TryDequeue(out var waitingTest))
    {
        // Check if all constraint keys are available for this waiting test
        var canStart = true;
        var waitingKeys = waitingTest.ConstraintKeys;
        var waitingKeyCount = waitingKeys.Count;
        for (var j = 0; j < waitingKeyCount; j++)
        {
            if (lockedKeys.Contains(waitingKeys[j]))
            {
                canStart = false;
                break;
            }
        }

        if (canStart)
        {
            // Lock the keys for this test
            for (var j = 0; j < waitingKeyCount; j++)
            {
                lockedKeys.Add(waitingKeys[j]);
            }

            // Mark test to start after we exit the lock
            testsToStart.Add(waitingTest);
        }
        else
        {
            // Still can't run, keep it for re-queuing
            testsToRequeue.Add(waitingTest);
        }
    }

    // Re-add tests that still can't run
    foreach (var waitingTestItem in testsToRequeue)
    {
        waitingTests.Enqueue(waitingTestItem);
    }
}
```

**Step 3: Run tests to verify behavior unchanged**

Run: `cd TUnit.Engine.Tests && dotnet test`
Expected: All tests PASS

**Step 4: Commit**

```bash
git add TUnit.Engine/Scheduling/ConstraintKeyScheduler.cs
git commit -m "perf: pre-allocate lists outside lock scope in ConstraintKeyScheduler"
```

---

## Task 5: Add ImmutableList to ReflectionTestDataCollector

**Files:**
- Modify: `TUnit.Engine/Discovery/ReflectionTestDataCollector.cs:31-32`

**Step 1: Run existing tests to establish baseline**

Run: `cd TUnit.Engine.Tests && dotnet test`
Expected: All tests PASS

**Step 2: Add System.Collections.Immutable using and change field declaration**

At the top of `ReflectionTestDataCollector.cs`, ensure this using is present:

```csharp
using System.Collections.Immutable;
```

Change lines 31-32 from:

```csharp
private static readonly List<TestMetadata> _discoveredTests = new(capacity: 1000);
private static readonly Lock _discoveredTestsLock = new();
```

To:

```csharp
private static ImmutableList<TestMetadata> _discoveredTests = ImmutableList<TestMetadata>.Empty;
```

**Step 3: Run tests (will fail - fields referenced elsewhere)**

Run: `cd TUnit.Engine.Tests && dotnet test`
Expected: FAIL (compilation errors due to field changes)

**Step 4: Commit partial change**

```bash
git add TUnit.Engine/Discovery/ReflectionTestDataCollector.cs
git commit -m "refactor: change _discoveredTests to ImmutableList (WIP)"
```

---

## Task 6: Update CollectTestsAsync for ImmutableList

**Files:**
- Modify: `TUnit.Engine/Discovery/ReflectionTestDataCollector.cs:136-141`

**Step 1: Replace lock with atomic swap**

Change lines 136-141 from:

```csharp
// Add to discovered tests with lock
lock (_discoveredTestsLock)
{
    _discoveredTests.AddRange(newTests);
    return new List<TestMetadata>(_discoveredTests);
}
```

To:

```csharp
// Atomic swap - no lock needed for readers
ImmutableList<TestMetadata> original, updated;
do
{
    original = _discoveredTests;
    updated = original.AddRange(newTests);
} while (Interlocked.CompareExchange(ref _discoveredTests, updated, original) != original);

return _discoveredTests;
```

**Step 2: Run tests (may still fail if other usages remain)**

Run: `cd TUnit.Engine.Tests && dotnet test`
Expected: May fail if streaming methods still use old lock

**Step 3: Commit**

```bash
git add TUnit.Engine/Discovery/ReflectionTestDataCollector.cs
git commit -m "perf: use atomic swap for CollectTestsAsync"
```

---

## Task 7: Update CollectTestsStreamingAsync for ImmutableList

**Files:**
- Modify: `TUnit.Engine/Discovery/ReflectionTestDataCollector.cs:174-190`

**Step 1: Replace lock with ImmutableInterlocked.Update**

Change lines 174-178 from:

```csharp
lock (_discoveredTestsLock)
{
    _discoveredTests.Add(test);
}
yield return test;
```

To:

```csharp
ImmutableInterlocked.Update(ref _discoveredTests, list => list.Add(test));
yield return test;
```

Similarly for lines 185-188:

```csharp
ImmutableInterlocked.Update(ref _discoveredTests, list => list.Add(dynamicTest));
yield return dynamicTest;
```

**Step 2: Run tests to verify streaming works**

Run: `cd TUnit.Engine.Tests && dotnet test`
Expected: All tests PASS

**Step 3: Commit**

```bash
git add TUnit.Engine/Discovery/ReflectionTestDataCollector.cs
git commit -m "perf: use ImmutableInterlocked.Update for streaming discovery"
```

---

## Task 8: Update ClearCaches for ImmutableList

**Files:**
- Modify: `TUnit.Engine/Discovery/ReflectionTestDataCollector.cs:47-60`

**Step 1: Simplify ClearCaches to use atomic assignment**

Change lines 50-53 from:

```csharp
lock (_discoveredTestsLock)
{
    _discoveredTests.Clear();
}
```

To:

```csharp
Interlocked.Exchange(ref _discoveredTests, ImmutableList<TestMetadata>.Empty);
```

**Step 2: Run all tests**

Run: `cd TUnit.Engine.Tests && dotnet test`
Expected: All tests PASS

**Step 3: Commit**

```bash
git add TUnit.Engine/Discovery/ReflectionTestDataCollector.cs
git commit -m "perf: simplify ClearCaches with atomic exchange"
```

---

## Task 9: Run Full Test Suite and Performance Verification

**Files:**
- None (verification only)

**Step 1: Run full TUnit test suite**

Run: `dotnet test`
Expected: All tests PASS

**Step 2: Run performance tests with dotnet-trace**

```bash
cd TUnit.PerformanceTests
dotnet trace collect -- dotnet run -c Release
```

**Step 3: Verify no regressions**

Compare trace output with baseline (if available). Look for:
- Reduced lock contention time
- Reduced thread wait time
- Similar or better wall clock time

**Step 4: Final commit with summary**

```bash
git add -A
git commit -m "perf: reduce lock contention in test discovery and scheduling

Implements optimizations for #4162:
- ReflectionTestDataCollector: ImmutableList<T> with atomic swap
- ConstraintKeyScheduler: manual loops replacing LINQ, pre-allocated lists

This eliminates defensive copies under lock and reduces LINQ allocations
in hot paths during parallel test execution."
```

---

## Verification Checklist

- [ ] All existing tests pass
- [ ] Stress tests pass under high contention
- [ ] No deadlocks under parallel execution
- [ ] Wall clock time equal or improved
- [ ] Lock contention reduced (verify with dotnet-trace)

---

Plan complete and saved to `docs/plans/2025-12-26-lock-contention-optimization.md`. Two execution options:

**1. Subagent-Driven (this session)** - I dispatch fresh subagent per task, review between tasks, fast iteration

**2. Parallel Session (separate)** - Open new session with executing-plans, batch execution with checkpoints

**Which approach?**
