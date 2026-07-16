#if NET
using System.Runtime.CompilerServices;

namespace TUnit.Core;

/// <summary>
/// Thread-safe registry that maps data source objects to their trace scope.
/// Used by the engine to parent initialization and disposal OpenTelemetry spans
/// under the correct activity (session, assembly, class, or test).
/// </summary>
/// <remarks>
/// Objects are registered by the engine (TestBuilder, PropertyInjector) when data source
/// attributes implement <see cref="ITraceScopeProvider"/>. The engine reads the scope
/// during initialization to determine the parent activity for each object's trace span.
/// Uses <see cref="ConditionalWeakTable{TKey,TValue}"/> so that per-test data source objects
/// can be garbage-collected after their tests complete, rather than being held alive for the
/// entire session. ConditionalWeakTable inherently uses reference equality.
/// </remarks>
internal static class TraceScopeRegistry
{
    private static readonly ConditionalWeakTable<object, StrongBox<SharedType>> Scopes = new();

    /// <summary>
    /// Registers trace scopes for data objects produced by a data source attribute.
    /// If the attribute implements <see cref="ITraceScopeProvider"/>, each object is
    /// paired with the corresponding <see cref="SharedType"/> from the provider.
    /// First registration wins — subsequent calls for the same instance are no-ops.
    /// </summary>
    internal static void RegisterFromDataSource(IDataSourceAttribute dataSource, object?[]? objects)
    {
        if (objects is null || objects.Length == 0)
        {
            return;
        }

        if (dataSource is not ITraceScopeProvider traceScopeProvider)
        {
            return;
        }

        using var enumerator = traceScopeProvider.GetSharedTypes().GetEnumerator();
        for (var i = 0; i < objects.Length; i++)
        {
            var sharedType = enumerator.MoveNext() ? enumerator.Current : SharedType.None;
            if (objects[i] is not null && !Scopes.TryGetValue(objects[i]!, out _))
            {
                // TryGetValue above is a fast-path to avoid allocating the closure below
                // on re-registration. GetValue is the atomic operation that enforces
                // first-registration-wins semantics.
                Scopes.GetValue(objects[i]!, _ => new StrongBox<SharedType>(sharedType));
            }
        }
    }

    /// <summary>
    /// Returns the <see cref="SharedType"/> for an object, or <c>null</c> if unregistered.
    /// Unregistered objects (e.g., the test class instance) default to per-test scope.
    /// </summary>
    internal static SharedType? GetSharedType(object obj)
    {
        return Scopes.TryGetValue(obj, out var box) ? box.Value : null;
    }
}
#endif
