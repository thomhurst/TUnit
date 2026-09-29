using TUnit.Core.Executors;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

/// <summary>
/// Synchronous hook bodies must keep async-method semantics when handed to a hook executor:
/// ExecutionContext changes made by the hook do not leak into the executor, and a synchronous
/// OperationCanceledException surfaces as a canceled (not faulted) task rather than a synchronous throw./// </summary>
[EngineTest(ExpectedResult.Pass)]
public class SyncHookBodySemanticsTests
{
    internal static readonly AsyncLocal<string?> HookLocal = new();

    internal static bool AsyncLocalHookRan;
    internal static string? ValueObservedByExecutor;

    internal static bool CancelingHookThrewSynchronously;
    internal static bool? CancelingHookTaskWasCanceled;

    [Before(Test)]
    [HookExecutor<AsyncLocalObservingExecutor>]
    public void SetAsyncLocalSynchronously()
    {
        AsyncLocalHookRan = true;
        HookLocal.Value = "set-by-hook";
    }

    [Before(Test)]
    [HookExecutor<CancellationObservingExecutor>]
    public void ThrowOperationCanceledSynchronously()
    {
        throw new OperationCanceledException(new CancellationToken(true));
    }

    [Test]
    public async Task SyncHook_AsyncLocal_DoesNotLeakIntoExecutor()
    {
        await Assert.That(AsyncLocalHookRan).IsTrue();
        await Assert.That(ValueObservedByExecutor).IsNull();
    }

    [Test]
    public async Task SyncHook_OperationCanceled_SurfacesAsCanceledTask()
    {
        await Assert.That(CancelingHookThrewSynchronously).IsFalse();

        // Reflection mode invokes hooks via MethodInfo.Invoke, which wraps the exception in a
        // TargetInvocationException, so only source-generated bodies can surface a canceled task.
        if (SourceRegistrar.IsEnabled)
        {
            await Assert.That(CancelingHookTaskWasCanceled).IsEqualTo(true);
        }
    }
}

public class AsyncLocalObservingExecutor : GenericAbstractExecutor
{
    protected override async ValueTask ExecuteAsync(Func<ValueTask> action)
    {
        var pending = action();
        SyncHookBodySemanticsTests.ValueObservedByExecutor = SyncHookBodySemanticsTests.HookLocal.Value;
        await pending;
    }
}

public class CancellationObservingExecutor : GenericAbstractExecutor
{
    protected override ValueTask ExecuteAsync(Func<ValueTask> action)
    {
        Task task;
        try
        {
            task = action().AsTask();
        }
        catch (OperationCanceledException)
        {
            SyncHookBodySemanticsTests.CancelingHookThrewSynchronously = true;
            return default;
        }

        SyncHookBodySemanticsTests.CancelingHookTaskWasCanceled = task.IsCanceled;

        // Observe and swallow the outcome so the hook itself does not fail the tests.
        _ = task.Exception;
        return default;
    }
}
