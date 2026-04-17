using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Executors;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

/// <summary>
/// Regression tests for https://github.com/thomhurst/TUnit/issues/5462 — HookExecutorAttribute
/// applied at class (and assembly) level must cascade to hooks declared in the scope, not just
/// to hooks where the attribute sits directly on the method. Mirrors CultureHookTests, which is
/// the analogous coverage for #5452.
/// </summary>
internal static class RecordingHookExecutorState
{
    // Each fixture uses its own executor type (and therefore its own bucket) so that
    // assertions are not affected by parallel-running fixtures.
    private static readonly ConcurrentDictionary<string, ConcurrentBag<string>> _invocations = new();

    public static void Record(string bucket, string hookName)
    {
        _invocations.GetOrAdd(bucket, _ => new ConcurrentBag<string>()).Add(hookName);
    }

    public static int Count(string bucket)
    {
        return _invocations.TryGetValue(bucket, out var bag) ? bag.Count : 0;
    }
}

public sealed class RecordingHookExecutor_F1ClassLevel : GenericAbstractExecutor
{
    protected override async ValueTask ExecuteAsync(Func<ValueTask> action)
    {
        RecordingHookExecutorState.Record(nameof(RecordingHookExecutor_F1ClassLevel), "executed");
        await action();
    }
}

public sealed class RecordingHookExecutor_F2ClassLevel : GenericAbstractExecutor
{
    protected override async ValueTask ExecuteAsync(Func<ValueTask> action)
    {
        RecordingHookExecutorState.Record(nameof(RecordingHookExecutor_F2ClassLevel), "executed");
        await action();
    }
}

public sealed class RecordingHookExecutor_F2MethodOverride : GenericAbstractExecutor
{
    protected override async ValueTask ExecuteAsync(Func<ValueTask> action)
    {
        RecordingHookExecutorState.Record(nameof(RecordingHookExecutor_F2MethodOverride), "executed");
        await action();
    }
}

public sealed class RecordingHookExecutor_F3Inherits : GenericAbstractExecutor
{
    protected override async ValueTask ExecuteAsync(Func<ValueTask> action)
    {
        RecordingHookExecutorState.Record(nameof(RecordingHookExecutor_F3Inherits), "executed");
        await action();
    }
}

[EngineTest(ExpectedResult.Pass)]
[HookExecutor<RecordingHookExecutor_F1ClassLevel>]
[NotInParallel(nameof(HookExecutorHookTests_ClassLevel))]
public class HookExecutorHookTests_ClassLevel
{
    [Before(Class)]
    public static Task BeforeClass()
    {
        return Task.CompletedTask;
    }

    [After(Class)]
    public static async Task AfterClass()
    {
        // Runs after the last test in this class — assert directly. By the end of the
        // class lifecycle the class-level [HookExecutor] must have wrapped at least
        // Before(Class), the per-test Before(Test)/After(Test) for both tests, and this
        // After(Class) hook itself. We just need ≥ 1 to confirm cascading worked at all.
        var count = RecordingHookExecutorState.Count(nameof(RecordingHookExecutor_F1ClassLevel));
        await Assert.That(count).IsGreaterThanOrEqualTo(1);
    }

    [Before(Test)]
    public Task BeforeTest()
    {
        return Task.CompletedTask;
    }

    [After(Test)]
    public Task AfterTest()
    {
        return Task.CompletedTask;
    }

    [Test]
    public async Task ClassLevelHookExecutor_RanForBeforeClassHook()
    {
        // Before(Class) ran before this test. With the class-level [HookExecutor<...>]
        // cascading, the recording executor must have been invoked at least once.
        var count = RecordingHookExecutorState.Count(nameof(RecordingHookExecutor_F1ClassLevel));
        await Assert.That(count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task ClassLevelHookExecutor_RanForBeforeTestHook()
    {
        // By the time this test body runs, Before(Test) has executed for it. The recording
        // executor should have been invoked at least twice — once for Before(Class) (shared
        // across both tests) and once for the most recent Before(Test).
        var count = RecordingHookExecutorState.Count(nameof(RecordingHookExecutor_F1ClassLevel));
        await Assert.That(count).IsGreaterThanOrEqualTo(2);
    }
}

[EngineTest(ExpectedResult.Pass)]
[HookExecutor<RecordingHookExecutor_F2ClassLevel>]
public class HookExecutorHookTests_MethodLevelOverride
{
    [Before(Test), HookExecutor<RecordingHookExecutor_F2MethodOverride>]
    public Task BeforeTest()
    {
        return Task.CompletedTask;
    }

    [Test]
    public async Task MethodLevel_HookExecutor_OverridesClassLevel()
    {
        // The Before(Test) hook above carries its own [HookExecutor<F2MethodOverride>],
        // which must beat the class-level [HookExecutor<F2ClassLevel>] for this hook.
        // Method-override executor must be invoked, class-level executor must not be —
        // there is no other hook in this fixture for the class-level executor to wrap.
        var methodOverrideCount = RecordingHookExecutorState.Count(nameof(RecordingHookExecutor_F2MethodOverride));
        await Assert.That(methodOverrideCount).IsGreaterThanOrEqualTo(1);

        var classLevelCount = RecordingHookExecutorState.Count(nameof(RecordingHookExecutor_F2ClassLevel));
        await Assert.That(classLevelCount).IsEqualTo(0);
    }
}

[EngineTest(ExpectedResult.Pass)]
[HookExecutor<RecordingHookExecutor_F3Inherits>]
public class HookExecutorHookTests_InheritsClassLevel
{
    // No method-level [HookExecutor] override — class-level RecordingHookExecutor_F3Inherits
    // applies to the Before(Test) hook.
    [Before(Test)]
    public Task BeforeTest()
    {
        return Task.CompletedTask;
    }

    [Test]
    public async Task InheritsClassHookExecutor_ForBeforeTest()
    {
        var count = RecordingHookExecutorState.Count(nameof(RecordingHookExecutor_F3Inherits));
        await Assert.That(count).IsGreaterThanOrEqualTo(1);
    }
}
