using System.Collections.Concurrent;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Services;

/// <summary>
/// Thread-safe service for initializing objects that implement <see cref="IAsyncInitializer"/>.
/// Provides deduplicated initialization with explicit phase control.
/// </summary>
/// <remarks>
/// <para>
/// This service replaces the static <see cref="ObjectInitializer"/> for dependency injection scenarios.
/// It uses <see cref="ConcurrentDictionary{TKey,TValue}"/> with reference equality for thread-safe
/// deduplication without lock contention during async operations.
/// </para>
/// </remarks>
public sealed class ObjectInitializationService : IObjectInitializationService
{
    // Use ConcurrentDictionary with reference equality for thread-safe tracking
    // This replaces ConditionalWeakTable which doesn't support explicit cleanup
    private readonly ConcurrentDictionary<object, Task> _initializationTasks;

    /// <summary>
    /// Creates a new instance of the initialization service.
    /// </summary>
    public ObjectInitializationService()
    {
        _initializationTasks = new ConcurrentDictionary<object, Task>(new Helpers.ReferenceEqualityComparer());
    }

    /// <inheritdoc />
    public ValueTask InitializeForDiscoveryAsync(object? obj, CancellationToken cancellationToken = default)
    {
        // During discovery, only initialize IAsyncDiscoveryInitializer
        if (obj is not IAsyncDiscoveryInitializer asyncDiscoveryInitializer)
        {
            return default;
        }

        return InitializeCoreAsync(obj, asyncDiscoveryInitializer, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask InitializeAsync(object? obj, CancellationToken cancellationToken = default)
    {
        if (obj is not IAsyncInitializer asyncInitializer)
        {
            return default;
        }

        return InitializeCoreAsync(obj, asyncInitializer, cancellationToken);
    }

    /// <inheritdoc />
    public bool IsInitialized(object? obj)
    {
        if (obj is not IAsyncInitializer)
        {
            return false;
        }

        // Use Status == RanToCompletion to ensure we don't return true for faulted/canceled tasks
        // (IsCompletedSuccessfully is not available in netstandard2.0)
        return _initializationTasks.TryGetValue(obj, out var task) && task.Status == TaskStatus.RanToCompletion;
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        _initializationTasks.Clear();
    }

    private async ValueTask InitializeCoreAsync(
        object obj,
        IAsyncInitializer asyncInitializer,
        CancellationToken cancellationToken)
    {
        // Use GetOrAdd for thread-safe deduplication without holding lock during async
        // Note: GetOrAdd's factory may be called multiple times under contention,
        // but only one result is stored. Multiple InitializeAsync() calls is safe
        // since we only await the winning task.
        var initializationTask = _initializationTasks.GetOrAdd(obj, _ => asyncInitializer.InitializeAsync());

        // Wait for initialization with cancellation support
        await initializationTask.WaitAsync(cancellationToken);
    }
}
