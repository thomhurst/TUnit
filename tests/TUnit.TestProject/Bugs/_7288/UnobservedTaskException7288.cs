using System.Runtime.CompilerServices;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7288;

/// <summary>
/// #7288: leaves a faulted task that nothing observes, then forces the garbage collection that
/// makes the runtime raise TaskScheduler.UnobservedTaskException. The test itself passes. The
/// session must report the exception, and must fail when run with
/// --fail-on-unobserved-task-exceptions.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class UnobservedTaskException7288
{
    public const string Message = "Intentional unobserved task exception (#7288)";

    [Test]
    public void LeavesFaultedTaskUnobserved()
    {
        CreateFaultedTask();

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // Separate, non-inlined method so no reference to the task survives into the collection.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateFaultedTask()
    {
        var source = new TaskCompletionSource<bool>();
        source.SetException(new InvalidOperationException(Message));
    }
}
