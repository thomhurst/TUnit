using System.Collections.Concurrent;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Core.Services;

namespace TUnit.Core;

/// <summary>
/// Static facade for initializing objects that implement <see cref="IAsyncInitializer"/>.
/// Provides thread-safe, deduplicated initialization with explicit phase control.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="InitializeForDiscoveryAsync"/> during test discovery - only <see cref="IAsyncDiscoveryInitializer"/> objects are initialized.
/// Use <see cref="InitializeAsync"/> during test execution - all <see cref="IAsyncInitializer"/> objects are initialized.
/// </para>
/// <para>
/// For dependency injection scenarios, use <see cref="ObjectInitializationService"/> directly.
/// </para>
/// </remarks>
public static class ObjectInitializer
{
    // Use ConcurrentDictionary with reference equality for thread-safe tracking
    // This replaces ConditionalWeakTable which doesn't support explicit cleanup
    private static readonly ConcurrentDictionary<object, Task> InitializationTasks =
        new(new Helpers.ReferenceEqualityComparer());

    /// <summary>
    /// Initializes an object during the discovery phase.
    /// Only objects implementing IAsyncDiscoveryInitializer are initialized.
    /// Regular IAsyncInitializer objects are skipped (deferred to execution phase).
    /// Thread-safe with deduplication - safe to call multiple times.
    /// </summary>
    /// <param name="obj">The object to potentially initialize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static ValueTask InitializeForDiscoveryAsync(object? obj, CancellationToken cancellationToken = default)
    {
        // During discovery, only initialize IAsyncDiscoveryInitializer
        if (obj is not IAsyncDiscoveryInitializer asyncDiscoveryInitializer)
        {
            return default;
        }

        return InitializeCoreAsync(obj, asyncDiscoveryInitializer, cancellationToken);
    }

    /// <summary>
    /// Initializes an object during the execution phase.
    /// All objects implementing IAsyncInitializer are initialized.
    /// Thread-safe with deduplication - safe to call multiple times.
    /// </summary>
    /// <param name="obj">The object to potentially initialize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static ValueTask InitializeAsync(object? obj, CancellationToken cancellationToken = default)
    {
        if (obj is not IAsyncInitializer asyncInitializer)
        {
            return default;
        }

        return InitializeCoreAsync(obj, asyncInitializer, cancellationToken);
    }

    /// <summary>
    /// Checks if an object has been successfully initialized by ObjectInitializer.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <returns>True if the object has been initialized successfully; otherwise, false.</returns>
    /// <remarks>
    /// Returns false if the object is null, not an <see cref="IAsyncInitializer"/>,
    /// has not been initialized yet, or if initialization failed.
    /// </remarks>
    internal static bool IsInitialized(object? obj)
    {
        if (obj is not IAsyncInitializer)
        {
            return false;
        }

        // Use Status == RanToCompletion to ensure we don't return true for faulted/canceled tasks
        // (IsCompletedSuccessfully is not available in netstandard2.0)
        return InitializationTasks.TryGetValue(obj, out var task) && task.Status == TaskStatus.RanToCompletion;
    }

    /// <summary>
    /// Clears the initialization cache.
    /// </summary>
    /// <remarks>
    /// Called at the end of a test session to release resources.
    /// </remarks>
    internal static void ClearCache()
    {
        InitializationTasks.Clear();
    }

    private static async ValueTask InitializeCoreAsync(
        object obj,
        IAsyncInitializer asyncInitializer,
        CancellationToken cancellationToken)
    {
        // Use GetOrAdd for thread-safe deduplication without holding lock during async
        // Note: GetOrAdd's factory may be called multiple times under contention,
        // but only one result is stored. Multiple InitializeAsync() calls is safe
        // since we only await the winning task.
        var initializationTask = InitializationTasks.GetOrAdd(obj, _ => asyncInitializer.InitializeAsync());

        // Wait for initialization with cancellation support
        await initializationTask.WaitAsync(cancellationToken);
    }
}
