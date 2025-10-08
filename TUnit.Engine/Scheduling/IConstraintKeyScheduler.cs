using System.Diagnostics.CodeAnalysis;
using TUnit.Core;

namespace TUnit.Engine.Scheduling;

internal interface IConstraintKeyScheduler
{
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    ValueTask ExecuteTestsWithConstraintsAsync(
        (AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys, int Priority)[] tests,
        CancellationToken cancellationToken);
}