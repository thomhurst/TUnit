using TUnit.Core;

namespace TUnit.Engine;

/// <summary>
/// Placeholder test that stands in for a data-driven test whose data source is marked
/// <c>DeferEnumeration = true</c>. A single one of these is produced during discovery so the IDE shows
/// one node instead of expanding potentially thousands of cases. At execution time it is never run as a
/// real test — <see cref="Services.DeferredTestExpander"/> expands it into the real test cases, which are
/// scheduled and reported nested under this placeholder. The Create/Invoke members therefore throw: if one
/// is ever reached it means the placeholder leaked into the normal execution path, which is a bug.
/// </summary>
internal sealed class DeferredEnumerationExecutableTest : AbstractExecutableTest
{
    public override Task<object> CreateInstanceAsync() =>
        throw new InvalidOperationException(
            $"Deferred enumeration placeholder '{TestId}' was scheduled directly. It must be expanded by DeferredTestExpander before execution.");

    public override Task InvokeTestAsync(object instance, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(
            $"Deferred enumeration placeholder '{TestId}' was invoked directly. It must be expanded by DeferredTestExpander before execution.");
}
