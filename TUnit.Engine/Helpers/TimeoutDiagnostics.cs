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
        ("SemaphoreSlim.Wait()", "SemaphoreSlim.Wait() (synchronous) was called. Consider using SemaphoreSlim.WaitAsync() instead."),
        ("ManualResetEvent.WaitOne", "ManualResetEvent.WaitOne was called. The event may never be signaled."),
        ("AutoResetEvent.WaitOne", "AutoResetEvent.WaitOne was called. The event may never be signaled."),
        ("Task.Wait()", "Task.Wait() (synchronous) was called inside an async context. This can cause deadlocks. Use 'await' instead."),
        (".Result", "Task.Result was accessed synchronously. This can cause deadlocks in async contexts. Use 'await' instead."),
        (".GetAwaiter().GetResult()", "GetAwaiter().GetResult() was called synchronously. This can cause deadlocks in async contexts. Use 'await' instead."),
        ("SpinWait", "A SpinWait was active. The condition being waited on may never become true."),
        ("Thread.Sleep", "Thread.Sleep was called. Consider using Task.Delay in async code."),
        ("Mutex.WaitOne", "Mutex.WaitOne was called. The mutex may be held by another thread or process."),
    ];

    /// <summary>
    /// Builds an enhanced timeout message that includes diagnostic information.
    /// </summary>
    /// <param name="baseMessage">The original timeout message.</param>
    /// <param name="executionTask">The task that was being executed when the timeout occurred.</param>
    /// <returns>An enhanced message with diagnostics appended.</returns>
    public static string BuildTimeoutDiagnosticsMessage(string baseMessage, Task? executionTask)
    {
        var sb = new StringBuilder(baseMessage);

        AppendTaskStatus(sb, executionTask);
        AppendStackTraceDiagnostics(sb);

        return sb.ToString();
    }

    private static void AppendTaskStatus(StringBuilder sb, Task? executionTask)
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

        if (executionTask.IsFaulted && executionTask.Exception is { } aggregateException)
        {
            sb.AppendLine();
            sb.Append("Task exception: ");

            foreach (var innerException in aggregateException.InnerExceptions)
            {
                sb.AppendLine();
                sb.Append("  ");
                sb.Append(innerException.GetType().Name);
                sb.Append(": ");
                sb.Append(innerException.Message);
            }
        }
    }

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
        sb.Append("--- Timeout Stack Trace ---");
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
