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
    /// data sources run against. A dependency injection scope created for a static property is disposed
    /// here, at session end, because the object tracker does not dispose the services the scope owns.
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

        foreach (var invocation in onDispose.InvocationList)
        {
            try
            {
                await invocation.InvokeAsync(context, null!).ConfigureAwait(false);
            }
            catch (Exception disposeEx)
            {
                exceptions.Add(disposeEx);
            }
        }
    }
}
