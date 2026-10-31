using System.Collections.Concurrent;
using TUnit.Core;

namespace TUnit.Engine.Services;

/// <summary>
/// Routes <see cref="TaskScheduler.UnobservedTaskException"/> events to the sessions that are
/// running. The event is raised on the finalizer thread, which carries none of the ambient
/// AsyncLocal contexts, so <see cref="TestSessionContext.Current"/> is null there and cannot be
/// used to find the session.
/// </summary>
internal static class UnobservedTaskExceptionTracker
{
    private static readonly ConcurrentDictionary<TestSessionContext, byte> s_activeSessions = new();

    public static void Register(TestSessionContext session) => s_activeSessions.TryAdd(session, 0);

    public static void Unregister(TestSessionContext session) => s_activeSessions.TryRemove(session, out _);

    public static void Record(AggregateException exception)
    {
        // The runtime wraps the task's exceptions in an AggregateException. Keep a single cause
        // unwrapped so reports show the real exception type.
        Exception reported = exception.InnerExceptions.Count == 1 ? exception.InnerExceptions[0] : exception;

        foreach (var session in s_activeSessions.Keys)
        {
            session.AddUnobservedTaskException(reported);
        }
    }
}
