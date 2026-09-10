namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines a contract for managing object initialization with phase awareness.
/// </summary>
/// <remarks>
/// <para>
/// This service provides thread-safe, deduplicated initialization of objects that implement
/// <see cref="IAsyncInitializer"/> or <see cref="IAsyncDiscoveryInitializer"/>.
/// </para>
/// <para>
/// The service supports two initialization phases:
/// <list type="bullet">
/// <item><description>Discovery phase: Only <see cref="IAsyncDiscoveryInitializer"/> objects are initialized</description></item>
/// <item><description>Execution phase: All <see cref="IAsyncInitializer"/> objects are initialized</description></item>
/// </list>
/// </para>
/// </remarks>
internal interface IObjectInitializationService
{
    /// <summary>
    /// Initializes an object during the execution phase.
    /// </summary>
    /// <param name="obj">The object to initialize. If null or not an <see cref="IAsyncInitializer"/>, no action is taken.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// This method is thread-safe and ensures that each object is initialized exactly once.
    /// Multiple concurrent calls for the same object will share the same initialization task.
    /// </para>
    /// </remarks>
    ValueTask InitializeAsync(object? obj, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes an object during the discovery phase.
    /// </summary>
    /// <param name="obj">The object to initialize. If null or not an <see cref="IAsyncDiscoveryInitializer"/>, no action is taken.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Only objects implementing <see cref="IAsyncDiscoveryInitializer"/> are initialized during discovery.
    /// Regular <see cref="IAsyncInitializer"/> objects are deferred to execution phase.
    /// </para>
    /// </remarks>
    ValueTask InitializeForDiscoveryAsync(object? obj, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an object has been successfully initialized.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <returns>True if the object has been initialized successfully; otherwise, false.</returns>
    /// <remarks>
    /// Returns false if the object is null, not an <see cref="IAsyncInitializer"/>,
    /// has not been initialized yet, or if initialization failed.
    /// </remarks>
    bool IsInitialized(object? obj);

    /// <summary>
    /// Clears the initialization cache.
    /// </summary>
    /// <remarks>
    /// Called at the end of a test session to release resources.
    /// </remarks>
    void ClearCache();
}
