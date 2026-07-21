namespace TUnit.Engine.Utilities;

/// <summary>
/// Allocation-light bounded-parallelism async map.
/// Results preserve source order. A fixed set of workers pulls items via an interlocked
/// cursor, so no partitioner, channel, or per-item task is allocated.
/// Sets smaller than <see cref="SequentialThreshold"/> run sequentially, where task
/// scheduling overhead would exceed the parallelization benefit.
/// </summary>
internal static class ParallelMap
{
    /// <summary>
    /// Minimum number of items before parallel processing is used.
    /// </summary>
    public const int SequentialThreshold = 8;

    /// <summary>
    /// Void counterpart of <see cref="SelectParallelAsync{TSource,TResult}"/> — same worker
    /// model without the results array. Takes a ValueTask action so synchronously-completing
    /// callees don't allocate a Task per item.
    /// </summary>
    public static async Task ForEachParallelAsync<TSource>(
        IReadOnlyList<TSource> source,
        Func<TSource, ValueTask> action,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken = default)
    {
        var count = source.Count;

        if (count == 0)
        {
            return;
        }

        var workerCount = Math.Min(maxDegreeOfParallelism, count);

        if (workerCount <= 1 || count < SequentialThreshold)
        {
            for (var i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await action(source[i]).ConfigureAwait(false);
            }

            return;
        }

        var cursor = -1;

        Func<Task> worker = WorkerAsync;
        var workers = new Task[workerCount];
        for (var w = 0; w < workerCount; w++)
        {
            workers[w] = Task.Run(worker, CancellationToken.None);
        }

        await Task.WhenAll(workers).ConfigureAwait(false);

        return;

        async Task WorkerAsync()
        {
            while (true)
            {
                var index = Interlocked.Increment(ref cursor);

                if (index >= count)
                {
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await action(source[index]).ConfigureAwait(false);
                }
                catch
                {
                    // Park the cursor so sibling workers stop pulling new items;
                    // Task.WhenAll surfaces this fault once in-flight items finish.
                    Volatile.Write(ref cursor, count);
                    throw;
                }
            }
        }
    }

    public static Task<TResult[]> SelectParallelAsync<TSource, TResult>(
        IReadOnlyList<TSource> source,
        Func<TSource, Task<TResult>> selector,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken = default)
        => ForParallelAsync(source.Count, index => selector(source[index]), maxDegreeOfParallelism, cancellationToken);

    public static async Task<TResult[]> ForParallelAsync<TResult>(
        int count,
        Func<int, Task<TResult>> selector,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken = default)
    {
        if (count == 0)
        {
            return [];
        }

        var results = new TResult[count];
        var workerCount = Math.Min(maxDegreeOfParallelism, count);

        if (workerCount <= 1 || count < SequentialThreshold)
        {
            for (var i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                results[i] = await selector(i).ConfigureAwait(false);
            }

            return results;
        }

        var cursor = -1;

        // Task.Run rather than invoking WorkerAsync directly: a selector that completes
        // synchronously would otherwise run the entire loop inline on this thread and
        // serialize all the other workers.
        Func<Task> worker = WorkerAsync;
        var workers = new Task[workerCount];
        for (var w = 0; w < workerCount; w++)
        {
            workers[w] = Task.Run(worker, CancellationToken.None);
        }

        await Task.WhenAll(workers).ConfigureAwait(false);

        return results;

        async Task WorkerAsync()
        {
            while (true)
            {
                var index = Interlocked.Increment(ref cursor);

                if (index >= count)
                {
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    results[index] = await selector(index).ConfigureAwait(false);
                }
                catch
                {
                    // Park the cursor so sibling workers stop pulling new items;
                    // Task.WhenAll surfaces this fault once in-flight items finish.
                    Volatile.Write(ref cursor, count);
                    throw;
                }
            }
        }
    }
}
