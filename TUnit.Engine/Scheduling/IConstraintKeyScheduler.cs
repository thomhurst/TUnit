using TUnit.Core;

namespace TUnit.Engine.Scheduling;

internal interface IConstraintKeyScheduler
{
    ValueTask ExecuteTestsWithConstraintsAsync(
        (AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys, int Priority)[] tests,
        CancellationToken cancellationToken);
}