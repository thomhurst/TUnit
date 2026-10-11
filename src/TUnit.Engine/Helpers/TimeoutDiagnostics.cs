using System.Text;

namespace TUnit.Engine.Helpers;

/// <summary>
/// Provides diagnostic information when a test or hook times out,
/// including stack trace capture and deadlock pattern detection.
/// </summary>
internal static class TimeoutDiagnostics
{
    /// <summary>
    /// Common synchronization patterns that may indicate a deadlock when found in a stack trace.
    /// </summary>
    private static readonly (string Pattern, string Hint)[] DeadlockPatterns =
    [
        ("Monitor.Enter", "A lock (Monitor.Enter) was being acquired. This may indicate a deadlock if another thread holds the lock."),
        ("Monitor.Wait", "Monitor.Wait was called. Ensure the corresponding Monitor.Pulse/PulseAll is reachable."),
        ("SemaphoreSlim.Wait", "SemaphoreSlim.Wait (synchronous) was called. Consider using SemaphoreSlim.WaitAsync() instead."),
        ("ManualResetEvent.WaitOne", "ManualResetEvent.WaitOne was called. The event may never be signaled."),
        ("AutoResetEvent.WaitOne", "AutoResetEvent.WaitOne was called. The event may never be signaled."),
        ("Task.Wait", "Task.Wait (synchronous) was called inside an async context. This can cause deadlocks. Use 'await' instead."),
        ("get_Result", "Task.Result was accessed synchronously. This can cause deadlocks in async contexts. Use 'await' instead."),
        ("TaskAwaiter", "GetAwaiter().GetResult() was called synchronously. This can cause deadlocks in async contexts. Use 'await' instead."),
        ("SpinWait", "A SpinWait was active. The condition being waited on may never become true."),
        ("Thread.Sleep", "Thread.Sleep was called. Consider using Task.Delay in async code."),
        ("Mutex.WaitOne", "Mutex.WaitOne was called. The mutex may be held by another thread or process."),
    ];

    /// <summary>
    /// Builds an enhanced timeout message that includes diagnostic information.
    /// </summary>
    /// <param name="baseMessage">The original timeout message.</param>
    /// <param name="executionTask">The task that was being executed when the timeout occurred.</param>
    /// <param name="executionException">An exception observed while the task handled timeout cancellation.</param>
    /// <returns>An enhanced message with diagnostics appended.</returns>
    public static string BuildTimeoutDiagnosticsMessage(
        string baseMessage,
        Task? executionTask,
        Exception? executionException = null)
    {
        var sb = new StringBuilder(baseMessage);

        AppendTaskStatus(sb, executionTask, executionException);
#if NET
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out _);
        ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out _);
        AppendThreadPoolDiagnostics(sb, ThreadPool.ThreadCount, maxWorkerThreads - availableWorkerThreads, ThreadPool.PendingWorkItemCount);
#endif
        AppendStackTraceDiagnostics(sb);

        return sb.ToString();
    }

    private static void AppendTaskStatus(StringBuilder sb, Task? executionTask, Exception? executionException)
    {
        if (executionTask is null)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine();
        sb.Append("--- Task Status: ");
        sb.Append(executionTask.Status);
        sb.Append(" ---");

        if (executionException is not null)
        {
            AppendTaskExceptionHeader(sb);
            AppendTaskException(sb, executionException);
        }
        else if (executionTask.IsFaulted && executionTask.Exception is { } aggregateException)
        {
            AppendTaskExceptionHeader(sb);
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                AppendTaskException(sb, innerException);
            }
        }
    }

    private static void AppendTaskExceptionHeader(StringBuilder sb)
    {
        sb.AppendLine();
        sb.Append("Task exception: ");
    }

    private static void AppendTaskException(StringBuilder sb, Exception exception)
    {
        sb.AppendLine();
        sb.Append("  ");
        sb.Append(exception.GetType().Name);
        sb.Append(": ");
        sb.Append(exception.Message);
    }

#if NET
    /// <summary>
    /// Appends a thread pool snapshot. A backlog of queued work means continuations and timer
    /// callbacks were waiting for a thread, so the timeout may be caused by other tests blocking.
    /// </summary>
    internal static void AppendThreadPoolDiagnostics(StringBuilder sb, int threadCount, int busyWorkerThreads, long pendingWorkItems)
    {
        ThreadPool.GetMinThreads(out var minWorkerThreads, out _);
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out _);

        sb.AppendLine();
        sb.AppendLine();
        sb.Append("--- Thread Pool ---");
        sb.AppendLine();
        sb.Append("Threads: ");
        sb.Append(threadCount);
        sb.Append(", busy: ");
        sb.Append(busyWorkerThreads);
        sb.Append(", pending work items: ");
        sb.Append(pendingWorkItems);
        sb.Append(", min worker threads: ");
        sb.Append(minWorkerThreads);
        sb.Append(", max worker threads: ");
        sb.Append(maxWorkerThreads);

        if (IsThreadPoolSaturated(threadCount, busyWorkerThreads, pendingWorkItems))
        {
            sb.AppendLine();
            sb.Append("The thread pool appears saturated. This timeout may be caused by blocking calls ");
            sb.Append("(.Result, .Wait(), Thread.Sleep, synchronous I/O) in other concurrently running tests ");
            sb.Append("rather than by this test. Use async APIs throughout, or limit concurrency for heavy tests ");
            sb.Append("with [ParallelLimiter<T>] or --maximum-parallel-tests.");
        }
    }

    /// <summary>
    /// Work is waiting and every pool thread is busy. Even one queued timer callback counts: it
    /// cannot run until a thread frees up.
    /// </summary>
    internal static bool IsThreadPoolSaturated(int threadCount, int busyWorkerThreads, long pendingWorkItems)
        => pendingWorkItems > 0 && busyWorkerThreads >= threadCount;
#endif

    private static void AppendStackTraceDiagnostics(StringBuilder sb)
    {
        string stackTrace;

        try
        {
            stackTrace = Environment.StackTrace;
        }
        catch
        {
            return;
        }

        if (string.IsNullOrEmpty(stackTrace))
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine();
        sb.Append("--- Timeout Handler Stack Trace ---");
        sb.AppendLine();
        sb.Append("Note: This is the timeout handler's stack trace, not the blocked test's stack.");
        sb.AppendLine();
        sb.Append(stackTrace);

        var hints = DetectDeadlockPatterns(stackTrace);

        if (hints.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.Append("--- Potential Deadlock Detected ---");

            foreach (var hint in hints)
            {
                sb.AppendLine();
                sb.Append("  * ");
                sb.Append(hint);
            }
        }
    }

    /// <summary>
    /// Scans a stack trace for common patterns that may indicate a deadlock.
    /// </summary>
    internal static List<string> DetectDeadlockPatterns(string stackTrace)
    {
        var hints = new List<string>();

        foreach (var (pattern, hint) in DeadlockPatterns)
        {
#if NET
            if (stackTrace.Contains(pattern, StringComparison.OrdinalIgnoreCase))
#else
            if (stackTrace.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
#endif
            {
                hints.Add(hint);
            }
        }

        return hints;
    }
}
