using TUnit.Core.Enums;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7088;

/// <summary>
/// Regression for https://github.com/thomhurst/TUnit/issues/7088.
/// Keyed [NotInParallel] tests must start in [ExecutionPriority] order (highest first),
/// then in ascending NotInParallel Order. Each test checks that the tests that started
/// before it are exactly the expected prefix, so a test that starts out of order fails.
/// </summary>
internal sealed class StartOrderProbe(params string[] expectedOrder)
{
    private readonly List<string> _started = [];
    private readonly object _lock = new();

    public async Task RecordAndVerifyAsync()
    {
        var testName = TestContext.Current!.Metadata.TestDetails.TestName;

        string[] startedSoFar;
        lock (_lock)
        {
            _started.Add(testName);
            startedSoFar = [.. _started];
        }

        await Assert.That(string.Join(", ", startedSoFar))
            .IsEqualTo(string.Join(", ", expectedOrder.Take(startedSoFar.Length)));
    }
}

[EngineTest(ExpectedResult.Pass)]
public class KeyedNotInParallelOrderTests
{
    private static readonly StartOrderProbe Probe = new(
        nameof(HighPriority_Order50),
        nameof(Order1),
        nameof(Order2),
        nameof(Order3),
        nameof(Order1000000000),
        nameof(DefaultOrder));

    [Test, NotInParallel("KeyedNotInParallelOrder", Order = 3)]
    public Task Order3() => Probe.RecordAndVerifyAsync();

    [Test, NotInParallel("KeyedNotInParallelOrder")]
    public Task DefaultOrder() => Probe.RecordAndVerifyAsync();

    [Test, NotInParallel("KeyedNotInParallelOrder", Order = 1_000_000_000)]
    public Task Order1000000000() => Probe.RecordAndVerifyAsync();

    [Test, NotInParallel("KeyedNotInParallelOrder", Order = 1)]
    public Task Order1() => Probe.RecordAndVerifyAsync();

    [Test, NotInParallel("KeyedNotInParallelOrder", Order = 50), ExecutionPriority(Priority.High)]
    public Task HighPriority_Order50() => Probe.RecordAndVerifyAsync();

    [Test, NotInParallel("KeyedNotInParallelOrder", Order = 2)]
    public Task Order2() => Probe.RecordAndVerifyAsync();
}

/// <summary>
/// Same as <see cref="KeyedNotInParallelOrderTests"/>, but inside a [ParallelGroup], which
/// routes the keyed tests through the constrained-parallel-group path.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[ParallelGroup("KeyedNotInParallelOrder.Group")]
public class GroupedKeyedNotInParallelOrderTests
{
    private static readonly StartOrderProbe Probe = new(
        nameof(HighPriority_Order50),
        nameof(Order1),
        nameof(Order2),
        nameof(Order3),
        nameof(Order1000000000),
        nameof(DefaultOrder));

    [Test, NotInParallel("GroupedKeyedNotInParallelOrder", Order = 3)]
    public Task Order3() => Probe.RecordAndVerifyAsync();

    [Test, NotInParallel("GroupedKeyedNotInParallelOrder")]
    public Task DefaultOrder() => Probe.RecordAndVerifyAsync();

    [Test, NotInParallel("GroupedKeyedNotInParallelOrder", Order = 1_000_000_000)]
    public Task Order1000000000() => Probe.RecordAndVerifyAsync();

    [Test, NotInParallel("GroupedKeyedNotInParallelOrder", Order = 1)]
    public Task Order1() => Probe.RecordAndVerifyAsync();

    [Test, NotInParallel("GroupedKeyedNotInParallelOrder", Order = 50), ExecutionPriority(Priority.High)]
    public Task HighPriority_Order50() => Probe.RecordAndVerifyAsync();

    [Test, NotInParallel("GroupedKeyedNotInParallelOrder", Order = 2)]
    public Task Order2() => Probe.RecordAndVerifyAsync();
}
