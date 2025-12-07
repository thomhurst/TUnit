using System.Runtime.CompilerServices;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Centralized service for initializing objects that implement IAsyncInitializer.
/// Provides thread-safe, deduplicated initialization with explicit phase control.
///
/// Use InitializeForDiscoveryAsync during test discovery - only IAsyncDiscoveryInitializer objects are initialized.
/// Use InitializeAsync during test execution - all IAsyncInitializer objects are initialized.
/// </summary>
public static class ObjectInitializer
{
    private static readonly ConditionalWeakTable<object, Task> _initializationTasks = new();
    private static readonly Lock _lock = new();

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
    /// Checks if an object has been initialized by ObjectInitializer.
    /// </summary>
    internal static bool IsInitialized(object? obj)
    {
        if (obj is not IAsyncInitializer)
        {
            return false;
        }

        lock (_lock)
        {
            return _initializationTasks.TryGetValue(obj, out var task) && task.IsCompleted;
        }
    }

    private static async ValueTask InitializeCoreAsync(
        object obj,
        IAsyncInitializer asyncInitializer,
        CancellationToken cancellationToken)
    {
        Task initializationTask;

        lock (_lock)
        {
            if (_initializationTasks.TryGetValue(obj, out var existingTask))
            {
                initializationTask = existingTask;
            }
            else
            {
                initializationTask = asyncInitializer.InitializeAsync();
                _initializationTasks.Add(obj, initializationTask);
            }
        }

        // Wait for initialization with cancellation support
        await initializationTask.WaitAsync(cancellationToken);
    }
}
