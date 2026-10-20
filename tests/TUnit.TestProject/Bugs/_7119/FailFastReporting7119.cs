using TUnit.Core.Enums;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7119;

/// <summary>
/// Run with --fail-fast and --maximum-parallel-tests 1. The unconstrained and keyed phases run
/// at the same time and share the single slot, so Fails (Critical priority, first in the
/// unconstrained phase) runs no later than second. Fail-fast then cancels the tests still
/// waiting for the slot, and the global [NotInParallel] phase never starts. Each test must be
/// reported exactly once: Fails as failed, every test fail-fast stopped as skipped, and no
/// extra "Unhandled exception" node for the failure. DataSourceFails fails while its data is
/// generated, so it is already failed before it runs; fail-fast stops it before it reaches the
/// coordinator, and it must still be reported as failed.
/// </summary>
[EngineTest(ExpectedResult.Failure)]
public class FailFastReporting7119
{
    [Test, ExecutionPriority(Priority.Critical)]
    public void Fails() => throw new InvalidOperationException("Intentional failure to trigger fail-fast (#7119)");

    [Test] public Task Parallel1() => Task.Delay(100);
    [Test] public Task Parallel2() => Task.Delay(100);

    [Test, NotInParallel("7119.keyed")] public Task Keyed1() => Task.Delay(100);
    [Test, NotInParallel("7119.keyed")] public Task Keyed2() => Task.Delay(100);

    [Test, NotInParallel] public Task Global1() => Task.Delay(100);
    [Test, NotInParallel] public Task Global2() => Task.Delay(100);

    public static IEnumerable<int> ThrowingData() =>
        throw new InvalidOperationException("Intentional data source failure (#7119)");

    [Test, MethodDataSource(nameof(ThrowingData))]
    public void DataSourceFails(int value)
    {
    }
}
