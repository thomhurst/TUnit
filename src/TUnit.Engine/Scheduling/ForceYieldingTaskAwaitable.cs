#if !NET8_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Stand-in for <c>ConfigureAwait(ConfigureAwaitOptions.ForceYielding)</c>, which needs .NET 8.
/// Never reports completion, so the awaiting method always yields, and resumes without a
/// captured context. Registering a continuation on an already-completed task queues it to the
/// thread pool rather than running it inline.
/// </summary>
internal readonly struct ForceYieldingTaskAwaitable(Task task) : ICriticalNotifyCompletion
{
    private readonly ConfiguredTaskAwaitable.ConfiguredTaskAwaiter _awaiter = task.ConfigureAwait(false).GetAwaiter();

    public ForceYieldingTaskAwaitable GetAwaiter() => this;

    public bool IsCompleted => false;

    public void GetResult() => _awaiter.GetResult();

    public void OnCompleted(Action continuation) => _awaiter.OnCompleted(continuation);

    public void UnsafeOnCompleted(Action continuation) => _awaiter.UnsafeOnCompleted(continuation);
}
#endif
