# Lock Contention Optimization Design

**Issue:** [#4162 - perf: reduce lock contention in test discovery and scheduling](https://github.com/thomhurst/TUnit/issues/4162)
**Date:** 2025-12-26
**Priority:** P1
**Goal:** Maximum parallelism - minimize lock duration at any cost

---

## Problem Statement

Two performance-critical code paths unnecessarily extend lock durations, creating bottlenecks in parallel test execution:

1. **ReflectionTestDataCollector.cs (lines 137-141):** Lock remains held while creating a full defensive copy of discovered tests
2. **ConstraintKeyScheduler.cs (lines 56-69, 151-190):** LINQ evaluation and list allocation happen within lock scope

These practices serialize operations that should execute in parallel, degrading throughput on multi-core systems.

---

## Solution Overview

| Component | Approach | Complexity |
|-----------|----------|------------|
| ReflectionTestDataCollector | ImmutableList<T> with atomic swap | Medium |
| ConstraintKeyScheduler | Manual loops + two-phase locking + pre-allocation | High |

---

## Design: ReflectionTestDataCollector

### Current Implementation (Problem)

```csharp
private static readonly List<TestMetadata> _discoveredTests = new(capacity: 1000);
private static readonly Lock _discoveredTestsLock = new();

public async Task<IEnumerable<TestMetadata>> CollectTestsAsync(string testSessionId)
{
    // ... discovery logic ...

    lock (_discoveredTestsLock)
    {
        _discoveredTests.AddRange(newTests);
        return new List<TestMetadata>(_discoveredTests); // O(n) copy under lock
    }
}
```

### Proposed Implementation

Replace `List<TestMetadata>` + lock with `ImmutableList<TestMetadata>` + atomic swap:

```csharp
private static ImmutableList<TestMetadata> _discoveredTests = ImmutableList<TestMetadata>.Empty;

public async Task<IEnumerable<TestMetadata>> CollectTestsAsync(string testSessionId)
{
    // ... discovery logic unchanged ...

    // Atomic swap - no lock needed for readers
    ImmutableList<TestMetadata> original, updated;
    do
    {
        original = _discoveredTests;
        updated = original.AddRange(newTests);
    } while (Interlocked.CompareExchange(ref _discoveredTests, updated, original) != original);

    return _discoveredTests; // Already immutable, no copy needed
}
```

For streaming writes, use `ImmutableInterlocked.Update()` helper:

```csharp
// In CollectTestsStreamingAsync
ImmutableInterlocked.Update(ref _discoveredTests, list => list.Add(test));
yield return test;
```

### Benefits

- Zero lock contention on reads (callers get immutable snapshot)
- Writes use lock-free CAS loop (brief spin during concurrent writes)
- Eliminates defensive copy entirely
- Thread-safe enumeration without locks

### Trade-offs

- Slightly higher allocation on writes (new immutable list per update)
- Discovery is infrequent compared to reads, so this is acceptable

---

## Design: ConstraintKeyScheduler

### Problem 1: LINQ Inside Lock (lines 56-69)

**Current:**
```csharp
lock (lockObject)
{
    canStart = !constraintKeys.Any(key => lockedKeys.Contains(key)); // LINQ allocation
    if (canStart)
    {
        foreach (var key in constraintKeys)
            lockedKeys.Add(key);
    }
}
```

**Proposed - Manual loop with indexer access:**
```csharp
lock (lockObject)
{
    canStart = true;
    var keyCount = constraintKeys.Count;
    for (var i = 0; i < keyCount; i++)
    {
        if (lockedKeys.Contains(constraintKeys[i]))
        {
            canStart = false;
            break; // Early exit
        }
    }

    if (canStart)
    {
        for (var i = 0; i < keyCount; i++)
            lockedKeys.Add(constraintKeys[i]);
    }
}
```

**Benefits:**
- No delegate allocation
- No enumerator allocation
- Early exit on first conflict

### Problem 2: Allocations and Extended Lock Scope (lines 149-190)

**Current:**
```csharp
var testsToStart = new List<...>(); // Outside lock - good

lock (lockObject)
{
    foreach (var key in constraintKeys)
        lockedKeys.Remove(key);

    var tempQueue = new List<...>(); // Allocation INSIDE lock - bad

    while (waitingTests.TryDequeue(out var waitingTest))
    {
        var canStart = !waitingTest.ConstraintKeys.Any(...); // LINQ inside lock
        // ... extensive work inside lock
    }

    foreach (var item in tempQueue)
        waitingTests.Enqueue(item);
}
```

**Proposed - Two-phase locking with pre-allocation:**

```csharp
// Pre-allocate outside any lock
var testsToStart = new List<(..., TaskCompletionSource<bool>)>(4);
var testsToRequeue = new List<(..., TaskCompletionSource<bool>)>(8);
var waitingSnapshot = new List<(..., TaskCompletionSource<bool>)>(8);

// Phase 1: Release keys and snapshot queue (single brief lock)
lock (lockObject)
{
    var keyCount = constraintKeys.Count;
    for (var i = 0; i < keyCount; i++)
        lockedKeys.Remove(constraintKeys[i]);

    while (waitingTests.TryDequeue(out var item))
        waitingSnapshot.Add(item);
}

// Phase 2: For each candidate, try to acquire keys (brief lock per candidate)
foreach (var waitingTest in waitingSnapshot)
{
    bool acquired;
    lock (lockObject)
    {
        acquired = true;
        var keys = waitingTest.ConstraintKeys;
        var keyCount = keys.Count;
        for (var i = 0; i < keyCount; i++)
        {
            if (lockedKeys.Contains(keys[i]))
            {
                acquired = false;
                break;
            }
        }

        if (acquired)
        {
            for (var i = 0; i < keyCount; i++)
                lockedKeys.Add(keys[i]);
        }
    }

    if (acquired)
        testsToStart.Add(waitingTest);
    else
        testsToRequeue.Add(waitingTest);
}

// Phase 3: Requeue non-starters (single brief lock)
if (testsToRequeue.Count > 0)
{
    lock (lockObject)
    {
        foreach (var item in testsToRequeue)
            waitingTests.Enqueue(item);
    }
}

// Phase 4: Signal starters (outside lock - no contention)
foreach (var test in testsToStart)
    test.StartSignal.SetResult(true);
```

### Benefits

- Multiple brief locks instead of one long lock
- Other threads can interleave between phases
- All allocations outside locks
- No LINQ allocations
- Early exit in availability checks

---

## Testing Strategy

### 1. Stress Tests for Thread Safety

Add to `TUnit.Engine.Tests`:

```csharp
[Test]
[Repeat(10)]
public async Task ReflectionTestDataCollector_ConcurrentDiscovery_NoRaceConditions()
{
    var tasks = Enumerable.Range(0, 50)
        .Select(_ => collector.CollectTestsAsync(Guid.NewGuid().ToString()));

    var results = await Task.WhenAll(tasks);

    await Assert.That(results.SelectMany(r => r).Distinct().Count())
        .IsGreaterThan(0);
}

[Test]
[Repeat(10)]
public async Task ConstraintKeyScheduler_HighContention_NoDeadlocks()
{
    var tests = CreateTestsWithOverlappingConstraints(100, overlapFactor: 0.3);

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    await scheduler.ExecuteTestsWithConstraintsAsync(tests, cts.Token);
}
```

### 2. Wall Clock Benchmarks

```csharp
[Test]
[Category("Performance")]
public async Task Benchmark_TestDiscovery_WallClock()
{
    var stopwatch = Stopwatch.StartNew();
    var tests = await collector.CollectTestsAsync(testSessionId);
    stopwatch.Stop();

    Console.WriteLine($"Discovery wall clock: {stopwatch.ElapsedMilliseconds}ms");
    Console.WriteLine($"Tests discovered: {tests.Count()}");
    Console.WriteLine($"Throughput: {tests.Count() / stopwatch.Elapsed.TotalSeconds:F0} tests/sec");
}

[Test]
[Category("Performance")]
public async Task Benchmark_ConstrainedExecution_WallClock()
{
    var tests = CreateTestsWithConstraints(count: 500, constraintOverlap: 0.3);

    var stopwatch = Stopwatch.StartNew();
    await scheduler.ExecuteTestsWithConstraintsAsync(tests, CancellationToken.None);
    stopwatch.Stop();

    Console.WriteLine($"Constrained execution wall clock: {stopwatch.ElapsedMilliseconds}ms");
}
```

### 3. Profiling with dotnet-trace

```bash
# Baseline (before changes)
dotnet trace collect --name TUnit.PerformanceTests -- dotnet run -c Release
mv trace.nettrace baseline.nettrace

# Optimized (after changes)
dotnet trace collect --name TUnit.PerformanceTests -- dotnet run -c Release
mv trace.nettrace optimized.nettrace
```

### 4. Key Metrics

| Scenario | Metric | Target |
|----------|--------|--------|
| Discovery (1000 tests) | Wall clock time | >=10% improvement |
| Constrained execution (500 tests, 30% overlap) | Wall clock time | >=15% improvement |
| High parallelism (16+ cores) | Scaling efficiency | Near-linear |
| Lock contention | Thread wait time | >=50% reduction |
| Memory | Hot path allocations | >=30% reduction |

---

## Risk Mitigation

### Race Condition Risks

| Risk | Mitigation |
|------|------------|
| ImmutableList CAS loop starvation | Add retry limit with fallback to lock; contention is rare during discovery |
| Lost updates in two-phase lock | Each phase is atomic; tests either start or requeue, never lost |
| Stale reads of `_discoveredTests` | Acceptable - immutable snapshots are always consistent |

### Behavioral Compatibility

| Concern | Mitigation |
|---------|------------|
| Test ordering changes | Discovery order was never guaranteed |
| API consumers expecting mutable list | Return type is `IEnumerable<T>`, already read-only contract |
| Constraint scheduling order | Priority ordering preserved |

### Rollback Strategy

Both changes are isolated:
- `ReflectionTestDataCollector`: Revert to `List<T>` + lock with single file change
- `ConstraintKeyScheduler`: Revert loop-by-loop if specific optimization causes issues

---

## Implementation Plan

### Incremental Rollout (Suggested Merge Order)

1. **PR 1: LINQ to manual loop replacements** (lowest risk)
   - Replace `.Any()` with manual loops in ConstraintKeyScheduler
   - Immediate benefit, minimal code change

2. **PR 2: Lock scope restructuring** (medium risk)
   - Two-phase locking in ConstraintKeyScheduler
   - Pre-allocate lists outside locks

3. **PR 3: ImmutableList migration** (medium risk)
   - Replace List<T> + lock with ImmutableList<T> in ReflectionTestDataCollector
   - Atomic swap pattern

This allows isolating any regressions to specific changes.

---

## Verification Checklist

- [ ] All existing tests pass
- [ ] Stress tests added and passing
- [ ] Wall clock benchmarks show improvement
- [ ] dotnet-trace shows reduced lock contention
- [ ] No deadlocks under high parallelism (16+ cores)
- [ ] Memory allocations reduced in hot paths
