namespace TUnit.Core.Helpers;

/// <summary>
/// Helper methods for parallel task execution without LINQ allocations.
/// Provides optimized patterns for executing async operations in parallel.
/// Exceptions are aggregated in AggregateException when multiple tasks fail.
/// </summary>
public static class ParallelTaskHelper
{
    /// <summary>
    /// Executes an async action for each item in an array, in parallel.
    /// Uses pre-allocated task array to avoid LINQ allocations.
    /// </summary>
    /// <typeparam name="T">The type of items to process.</typeparam>
    /// <param name="items">The array of items to process.</param>
    /// <param name="action">The async action to execute for each item.</param>
    /// <returns>A task that completes when all items have been processed.</returns>
    public static async Task ForEachAsync<T>(T[] items, Func<T, Task> action)
    {
        if (items.Length == 0)
        {
            return;
        }

        var tasks = new Task[items.Length];
        for (var i = 0; i < items.Length; i++)
        {
            tasks[i] = action(items[i]);
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Executes an async action for each item in an array, in parallel, with cancellation support.
    /// Uses pre-allocated task array to avoid LINQ allocations.
    /// </summary>
    /// <typeparam name="T">The type of items to process.</typeparam>
    /// <param name="items">The array of items to process.</param>
    /// <param name="action">The async action to execute for each item.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when all items have been processed.</returns>
    public static async Task ForEachAsync<T>(T[] items, Func<T, CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        if (items.Length == 0)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var tasks = new Task[items.Length];
        for (var i = 0; i < items.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            tasks[i] = action(items[i], cancellationToken);
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Executes an async action for each item in an array, in parallel, with an index.
    /// Uses pre-allocated task array to avoid LINQ allocations.
    /// </summary>
    /// <typeparam name="T">The type of items to process.</typeparam>
    /// <param name="items">The array of items to process.</param>
    /// <param name="action">The async action to execute for each item with its index.</param>
    /// <returns>A task that completes when all items have been processed.</returns>
    public static async Task ForEachWithIndexAsync<T>(T[] items, Func<T, int, Task> action)
    {
        if (items.Length == 0)
        {
            return;
        }

        var tasks = new Task[items.Length];
        for (var i = 0; i < items.Length; i++)
        {
            tasks[i] = action(items[i], i);
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Executes an async action for each item in an array, in parallel, with an index and cancellation support.
    /// Uses pre-allocated task array to avoid LINQ allocations.
    /// </summary>
    /// <typeparam name="T">The type of items to process.</typeparam>
    /// <param name="items">The array of items to process.</param>
    /// <param name="action">The async action to execute for each item with its index.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when all items have been processed.</returns>
    public static async Task ForEachWithIndexAsync<T>(T[] items, Func<T, int, CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        if (items.Length == 0)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var tasks = new Task[items.Length];
        for (var i = 0; i < items.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            tasks[i] = action(items[i], i, cancellationToken);
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Executes an async action for each item in a list, in parallel.
    /// Uses pre-allocated task array to avoid LINQ allocations.
    /// </summary>
    /// <typeparam name="T">The type of items to process.</typeparam>
    /// <param name="items">The list of items to process.</param>
    /// <param name="action">The async action to execute for each item.</param>
    /// <returns>A task that completes when all items have been processed.</returns>
    public static async Task ForEachAsync<T>(IReadOnlyList<T> items, Func<T, Task> action)
    {
        if (items.Count == 0)
        {
            return;
        }

        var tasks = new Task[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            tasks[i] = action(items[i]);
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Executes an async action for each item in a list, in parallel, with cancellation support.
    /// Uses pre-allocated task array to avoid LINQ allocations.
    /// </summary>
    /// <typeparam name="T">The type of items to process.</typeparam>
    /// <param name="items">The list of items to process.</param>
    /// <param name="action">The async action to execute for each item.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when all items have been processed.</returns>
    public static async Task ForEachAsync<T>(IReadOnlyList<T> items, Func<T, CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var tasks = new Task[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            tasks[i] = action(items[i], cancellationToken);
        }

        await Task.WhenAll(tasks);
    }
}
