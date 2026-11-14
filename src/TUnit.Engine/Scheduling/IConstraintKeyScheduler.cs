using System.Diagnostics.CodeAnalysis;
using TUnit.Core;

namespace TUnit.Engine.Scheduling;

internal interface IConstraintKeyScheduler
{
    [RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    ValueTask ExecuteTestsWithConstraintsAsync(
        (AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys, int Priority)[] tests,
        CancellationToken cancellationToken);
}