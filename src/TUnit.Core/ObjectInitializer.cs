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
internal static class ObjectInitializer
{
    // One task per object, published before any user code runs, so InitializeAsync is called
    // exactly once per object even under contention. No lock is held while InitializeAsync runs:
    // Lazy<Task> + ExecutionAndPublication ran its synchronous part under a lock, so every other
    // caller blocked a thread-pool thread until it finished, starving the pool when that part
    // blocked on async work itself (#6904).
    private static readonly ConcurrentDictionary<object, Task> InitializationTasks =
        new(Helpers.ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Initializes an object during the discovery phase.
    /// Only objects implementing IAsyncDiscoveryInitializer are initialized.
    /// Regular IAsyncInitializer objects are skipped (deferred to execution phase).
    /// Thread-safe with deduplication - safe to call multiple times.
    /// </summary>
    /// <param name="obj">The object to potentially initialize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    internal static ValueTask InitializeForDiscoveryAsync(object? obj, CancellationToken cancellationToken = default)
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
    internal static ValueTask InitializeAsync(object? obj, CancellationToken cancellationToken = default)
    {
        if (obj is ITestAttemptInitializer attemptInitializer && TestContext.Current is { } context)
        {
            return attemptInitializer.InitializeForTestAttemptAsync(context, cancellationToken);
        }

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
        if (obj is ITestAttemptInitializer attemptInitializer)
        {
            return attemptInitializer.IsInitialized;
        }

        if (obj is not IAsyncInitializer)
        {
            return false;
        }

        // Use Status == RanToCompletion to ensure we don't return true for pending or failed initializations
        // (IsCompletedSuccessfully is not available in netstandard2.0)
        return InitializationTasks.TryGetValue(obj, out var initializationTask) &&
               initializationTask.Status == TaskStatus.RanToCompletion;
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

    // Kept async (rather than returning the WaitAsync task as a ValueTask) so that an
    // OperationCanceledException thrown by InitializeAsync still completes callers' tasks as
    // Canceled, as it did before, instead of Faulted.
    private static async ValueTask InitializeCoreAsync(
        object obj,
        IAsyncInitializer asyncInitializer,
        CancellationToken cancellationToken)
    {
        if (!InitializationTasks.TryGetValue(obj, out var initializationTask))
        {
            // Waiting tests must not run inline on the thread that completes initialization.
            var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            initializationTask = InitializationTasks.GetOrAdd(obj, completionSource.Task);

            if (ReferenceEquals(initializationTask, completionSource.Task))
            {
                // Only the caller that published the task runs InitializeAsync - inline up to its
                // first await, as before, but with no lock held (#6904).
                _ = RunInitializerAsync(asyncInitializer, completionSource);
            }
        }

        // Do NOT remove faulted tasks from the cache - subsequent callers get the same error
        // immediately. Removing and retrying can cause hangs when InitializeAsync partially
        // initialized resources (e.g. started ports/processes) that block re-initialization (#4715).
        // The cancellation token only stops this caller waiting; the initialization keeps running.
        await initializationTask.WaitAsync(cancellationToken);
    }

    private static async Task RunInitializerAsync(IAsyncInitializer asyncInitializer, TaskCompletionSource<bool> completionSource)
    {
        try
        {
            await asyncInitializer.InitializeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // SetException rather than SetCanceled, so callers get the original exception object -
            // including an OperationCanceledException thrown by InitializeAsync.
            completionSource.SetException(ex);
            return;
        }

        completionSource.SetResult(true);
    }
}
