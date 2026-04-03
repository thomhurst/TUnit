#if NET
using System.Collections.Concurrent;
using TUnit.Core.Helpers;

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
/// Uses reference equality to distinguish distinct instances that may compare equal.
/// </remarks>
internal static class TraceScopeRegistry
{
    // Fully qualify to disambiguate from System.Collections.Generic.ReferenceEqualityComparer
    private static readonly ConcurrentDictionary<object, SharedType> Scopes =
        new(Helpers.ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Records the <see cref="SharedType"/> for an object created by a class data source.
    /// </summary>
    internal static void Register(object obj, SharedType sharedType)
    {
        Scopes.TryAdd(obj, sharedType);
    }

    /// <summary>
    /// Registers trace scopes for data objects produced by a data source attribute.
    /// If the attribute implements <see cref="ITraceScopeProvider"/>, each object is
    /// paired with the corresponding <see cref="SharedType"/> from the provider.
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
            if (objects[i] is not null)
            {
                Scopes.TryAdd(objects[i]!, sharedType);
            }
        }
    }

    /// <summary>
    /// Returns the <see cref="SharedType"/> for an object, or <c>null</c> if unregistered.
    /// Unregistered objects (e.g., the test class instance) default to per-test scope.
    /// </summary>
    internal static SharedType? GetSharedType(object obj)
    {
        return Scopes.TryGetValue(obj, out var scope) ? scope : null;
    }

    /// <summary>
    /// Clears all entries. Called at end of test session.
    /// </summary>
    internal static void Clear()
    {
        Scopes.Clear();
    }
}
#endif
