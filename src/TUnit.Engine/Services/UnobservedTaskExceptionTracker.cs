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

    public static void Record(AggregateException exception) => Record(exception, s_activeSessions.Keys);

    internal static void Record(AggregateException exception, ICollection<TestSessionContext> sessions)
    {
        // The event names no session. With one session running, the fault is attributed to it.
        // With several (MTP server mode), every session reports it, but none fails because of it.
        var attributable = sessions.Count == 1;

        // The runtime wraps the task's exceptions in an AggregateException. Record each cause on
        // its own, so every report keeps each cause's type and stack trace.
        RecordCauses(exception, sessions, attributable);
    }

    // Not AggregateException.Flatten(): it drops an empty AggregateException, so a task that threw
    // one would record nothing. Only aggregates that wrap causes are unwrapped here.
    private static void RecordCauses(AggregateException exception, ICollection<TestSessionContext> sessions, bool attributable)
    {
        foreach (var cause in exception.InnerExceptions)
        {
            if (cause is AggregateException { InnerExceptions.Count: > 0 } nested)
            {
                RecordCauses(nested, sessions, attributable);
                continue;
            }

            foreach (var session in sessions)
            {
                session.AddUnobservedTaskException(cause, attributable);
            }
        }
    }
}
