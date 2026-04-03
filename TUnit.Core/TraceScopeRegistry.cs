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
/// Objects are registered in <see cref="ClassDataSources"/> based on their
/// <see cref="SharedType"/>. The engine reads the scope during initialization
/// to determine the parent activity for each object's trace span.
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
