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

    private static async ValueTask InitializeCoreAsync(
        object obj,
        IAsyncInitializer asyncInitializer,
        CancellationToken cancellationToken)
    {
        if (!InitializationTasks.TryGetValue(obj, out var initializationTask))
        {
            // RunContinuationsAsynchronously: when the initialization completes, the continuations of
            // callers waiting on a shared object are queued, rather than run one after another inline
            // on the thread that completed it.
            var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            initializationTask = InitializationTasks.GetOrAdd(obj, completionSource.Task);

            if (ReferenceEquals(initializationTask, completionSource.Task))
            {
                // Only the caller that published the task runs InitializeAsync - inline, as before - and it
                // awaits it directly, so it pays no extra thread-pool hop or async frame for publishing it.
                var initializerTask = StartInitializer(asyncInitializer);

                try
                {
                    // ConfigureAwait(false): publishing the result for other callers mustn't wait for this
                    // caller's context; this caller's own await still resumes on it.
                    await initializerTask.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // Publish the initialization's own outcome - not this caller's cancellation - now,
                    // or once it completes if only this caller stopped waiting.
                    _ = PublishOutcomeAsync(initializerTask, completionSource);
                    throw;
                }

                completionSource.SetResult(true);
                return;
            }
        }

        // Do NOT remove faulted tasks from the cache - subsequent callers get the same error
        // immediately. Removing and retrying can cause hangs when InitializeAsync partially
        // initialized resources (e.g. started ports/processes) that block re-initialization (#4715).
        // The cancellation token only stops this caller waiting; the initialization keeps running.
        await initializationTask.WaitAsync(cancellationToken);
    }

    private static Task StartInitializer(IAsyncInitializer asyncInitializer)
    {
        try
        {
            return asyncInitializer.InitializeAsync()
                ?? throw new InvalidOperationException($"{asyncInitializer.GetType().FullName}.InitializeAsync() returned null.");
        }
        catch (Exception ex)
        {
            // Non-async implementations can throw synchronously (and a null task is a bug in the
            // initializer) - treat either like a faulted initialization.
            return Task.FromException(ex);
        }
    }

    private static async Task PublishOutcomeAsync(Task initializerTask, TaskCompletionSource<bool> completionSource)
    {
        try
        {
            await initializerTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // SetException rather than SetCanceled, so callers get the original exception object -
            // including an OperationCanceledException thrown by InitializeAsync.
            completionSource.SetException(ex);

            // The failure itself was observed above; this copy exists for other callers, so don't report
            // it as unobserved if none of them ever awaits it.
            _ = completionSource.Task.Exception;
            return;
        }

        completionSource.SetResult(true);
    }
}
