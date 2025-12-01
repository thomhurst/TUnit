using System.Collections.Concurrent;

namespace TUnit.Core.Lifecycle;

/// <summary>
/// Unified interface for managing object lifecycle across TUnit.
/// This is the single source of truth for all object states, replacing
/// the fragmented initialization tracking in ObjectInitializer,
/// DataSourceInitializer, and PropertyInjectionCache.
///
/// Design principles:
/// - Single source of truth for object lifecycle state
/// - Explicit phase transitions
/// - Thread-safe operations
/// - Session-scoped cleanup support
/// </summary>
public interface IObjectLifecycleManager
{
    /// <summary>
    /// Ensures an object is at least in the Registered phase.
    /// This is called during test discovery when data sources are created.
    /// </summary>
    /// <param name="instance">The object to register.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask EnsureRegisteredAsync(object instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures an object has had its properties injected.
    /// If already injected, returns immediately.
    /// </summary>
    /// <param name="instance">The object to inject properties into.</param>
    /// <param name="objectBag">Shared object bag for the test context.</param>
    /// <param name="methodMetadata">Optional method metadata for resolution.</param>
    /// <param name="events">Events for disposal tracking.</param>
    /// <param name="preResolvedValues">Pre-resolved property values to use instead of creating new ones.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask EnsurePropertiesInjectedAsync(
        object instance,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata = null,
        TestContextEvents? events = null,
        IDictionary<string, object?>? preResolvedValues = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures an object is fully initialized (properties injected + IAsyncInitializer called).
    /// If already initialized, returns immediately.
    /// </summary>
    /// <param name="instance">The object to initialize.</param>
    /// <param name="objectBag">Shared object bag for the test context.</param>
    /// <param name="methodMetadata">Optional method metadata for resolution.</param>
    /// <param name="events">Events for disposal tracking.</param>
    /// <param name="preResolvedValues">Pre-resolved property values to use instead of creating new ones.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask EnsureInitializedAsync(
        object instance,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata = null,
        TestContextEvents? events = null,
        IDictionary<string, object?>? preResolvedValues = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an object as active and increments its reference count.
    /// Called when a test starts using an object.
    /// </summary>
    /// <param name="instance">The object to activate.</param>
    void IncrementReferenceCount(object instance);

    /// <summary>
    /// Decrements an object's reference count.
    /// When count reaches 0, the object is disposed.
    /// </summary>
    /// <param name="instance">The object to release.</param>
    ValueTask DecrementReferenceCountAsync(object instance);

    /// <summary>
    /// Gets the current lifecycle phase of an object.
    /// </summary>
    /// <param name="instance">The object to check.</param>
    /// <returns>The current lifecycle phase.</returns>
    ObjectLifecyclePhase GetPhase(object instance);

    /// <summary>
    /// Checks if an object is at least at the specified phase.
    /// </summary>
    /// <param name="instance">The object to check.</param>
    /// <param name="phase">The minimum phase to check for.</param>
    /// <returns>True if the object is at or past the specified phase.</returns>
    bool IsAtLeast(object instance, ObjectLifecyclePhase phase);

    /// <summary>
    /// Registers a callback to be invoked when an object is disposed.
    /// </summary>
    /// <param name="instance">The object to watch.</param>
    /// <param name="callback">Callback to invoke on disposal.</param>
    void OnDisposed(object instance, Action callback);

    /// <summary>
    /// Registers an async callback to be invoked when an object is disposed.
    /// </summary>
    /// <param name="instance">The object to watch.</param>
    /// <param name="callback">Async callback to invoke on disposal.</param>
    void OnDisposedAsync(object instance, Func<Task> callback);

    /// <summary>
    /// Clears all tracked state for the current session.
    /// Should be called at the end of a test session.
    /// </summary>
    void ClearSession();
}
