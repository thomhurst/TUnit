using TUnit.Core;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services.TestExecution;

/// <summary>
/// Raises a test's OnDispose callbacks once, after its final attempt, whether it ran, was retried,
/// was skipped or failed before it started. Data sources subscribe OnDispose while their row is fetched
/// (a dependency injection scope's disposal, for example), and every attempt reuses that row's
/// objects, so they must outlive every attempt and still be released when the test never runs (#7205).
/// </summary>
internal static class TestOnDisposeRaiser
{
    /// <summary>
    /// Raises the callbacks unless they were already raised for <paramref name="test"/>. TestCoordinator
    /// calls this for every test that reaches it. The scheduler calls it for tests it skips or cancels
    /// before they reach the coordinator: a test skipped because a dependency failed, and a test the
    /// run's cancellation stopped before it started.
    /// </summary>
    public static async ValueTask RaiseOnceAsync(AbstractExecutableTest test, TUnitFrameworkLogger logger)
    {
        var onDispose = test.Context.InternalEventsIfCreated?.OnDispose;
        if (onDispose?.InvocationList is not { } invocations || !test.TryClaimOnDispose())
        {
            return;
        }

        foreach (var invocation in invocations)
        {
            try
            {
                await invocation.InvokeAsync(test.Context, test.Context).ConfigureAwait(false);
            }
            catch (Exception disposeEx)
            {
#if NET
                TUnitActivitySource.RecordException(System.Diagnostics.Activity.Current, disposeEx);
#endif
                await logger.LogErrorAsync($"Error during OnDispose for {test.TestId}: {disposeEx}").ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Raises, once, the OnDispose callbacks that data sources subscribed on a builder context that no
    /// test owns: <see cref="TestSessionContext.GlobalStaticPropertyContext"/>, which static property
    /// data sources run against (and which receives the callbacks <see cref="DeferToSessionEnd"/> moves when
    /// no session is current), or the context of a class row fetch that failed before any test took it.
    /// A dependency injection scope created for a static property is disposed here, at session end,
    /// because the object tracker does not dispose the services the scope owns.
    /// No test exists, so the callbacks receive the builder context as sender and no test context.
    /// Handler exceptions are added to <paramref name="exceptions"/>.
    /// </summary>
    public static async ValueTask RaiseForSessionAsync(TestBuilderContext context, List<Exception> exceptions)
    {
        if (context.EventsIfCreated is not { OnDispose: { } onDispose } events)
        {
            return;
        }

        // Detach first so the callbacks run once even if this is reached again.
        events.OnDispose = null;

        await RaiseDetachedAsync(context, onDispose.InvocationList, exceptions).ConfigureAwait(false);
    }

    // Guards adds to the session context's OnDispose from discovery, which can run in parallel.
    private static readonly object s_sessionCallbacksLock = new();

    /// <summary>
    /// Moves the OnDispose callbacks subscribed on <paramref name="context"/> to the current session's
    /// <see cref="TestSessionContext.DeferredDiscoveryOnDispose"/>, so <see cref="RaiseDeferredForSessionAsync"/>
    /// raises them once when that session ends. Reflection discovery evaluates a generic class's data sources
    /// to infer its type arguments, and every test built from that partition receives the same objects, so
    /// a dependency injection scope created there lives for the session rather than for one test (#7205).
    /// Without a current session they go to <see cref="TestSessionContext.GlobalStaticPropertyContext"/>,
    /// which the session's static property cleanup raises.
    /// </summary>
    public static void DeferToSessionEnd(TestBuilderContext context)
    {
        if (context.EventsIfCreated is not { OnDispose: { } onDispose } events)
        {
            return;
        }

        events.OnDispose = null;

        lock (s_sessionCallbacksLock)
        {
            AsyncEvent<TestContext> target;
            if (TestSessionContext.Current is { } session)
            {
                target = session.DeferredDiscoveryOnDispose ??= new AsyncEvent<TestContext>();
            }
            else
            {
                var globalEvents = TestSessionContext.GlobalStaticPropertyContext.Events;
                target = globalEvents.OnDispose ??= new AsyncEvent<TestContext>();
            }

            foreach (var invocation in onDispose.InvocationList)
            {
                target.Add(invocation.InvokeAsync, invocation.Order);
            }
        }
    }

    /// <summary>
    /// Raises, once, the callbacks <see cref="DeferToSessionEnd"/> moved to <paramref name="session"/>.
    /// Called from the session's unconditional final cleanup, so they run even when no test executes
    /// (every test filtered out or removed by circular-dependency detection), and again when the session's
    /// services are disposed, which covers discovery-only sessions. Handler errors are logged.
    /// </summary>
    public static async ValueTask RaiseDeferredForSessionAsync(TestSessionContext session, TUnitFrameworkLogger logger)
    {
        AsyncEvent<TestContext>? onDispose;
        lock (s_sessionCallbacksLock)
        {
            onDispose = session.DeferredDiscoveryOnDispose;
            session.DeferredDiscoveryOnDispose = null;
        }

        if (onDispose is null)
        {
            return;
        }

        var exceptions = new List<Exception>();
        await RaiseDetachedAsync(session, onDispose.InvocationList, exceptions).ConfigureAwait(false);

        foreach (var exception in exceptions)
        {
            await logger.LogErrorAsync($"Error during OnDispose for data that discovery shared across a session: {exception}").ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Raises OnDispose callbacks that the caller already detached from their events, so no test raises
    /// them again: those of a builder context that no test owns, or those that a row which no test
    /// takes subscribed. No test exists, so the callbacks receive <paramref name="sender"/> and no test
    /// context. Handler exceptions are added to <paramref name="exceptions"/>.
    /// </summary>
    public static async ValueTask RaiseDetachedAsync(
        object sender,
        IReadOnlyList<AsyncEvent<TestContext>.Invocation> invocations,
        List<Exception> exceptions)
    {
        foreach (var invocation in invocations)
        {
            try
            {
                await invocation.InvokeAsync(sender, null!).ConfigureAwait(false);
            }
            catch (Exception disposeEx)
            {
                exceptions.Add(disposeEx);
            }
        }
    }
}
