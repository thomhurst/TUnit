using System.Collections.Concurrent;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines a callback interface for object initialization during property injection.
/// </summary>
/// <remarks>
/// <para>
/// This interface is used to break circular dependencies between property injection
/// and initialization services. Property injectors can call back to the initialization
/// service without directly depending on it.
/// </para>
/// </remarks>
internal interface IInitializationCallback
{
    /// <summary>
    /// Ensures an object is fully initialized (property injection + IAsyncInitializer).
    /// </summary>
    /// <typeparam name="T">The type of object to initialize.</typeparam>
    /// <param name="obj">The object to initialize.</param>
    /// <param name="objectBag">Shared object bag for the test context.</param>
    /// <param name="methodMetadata">Method metadata for the test. Can be null.</param>
    /// <param name="events">Test context events for tracking.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The initialized object.</returns>
    ValueTask<T> EnsureInitializedAsync<T>(
        T obj,
        ConcurrentDictionary<string, object?>? objectBag = null,
        MethodMetadata? methodMetadata = null,
        TestContextEvents? events = null,
        CancellationToken cancellationToken = default) where T : notnull;
}
