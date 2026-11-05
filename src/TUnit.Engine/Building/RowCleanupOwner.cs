using System.Collections.Concurrent;
using TUnit.Core;

namespace TUnit.Engine.Building;

/// <summary>
/// Owns a row's cleanup until one test or the session claims it. A row that never reaches
/// either owner is released by the builder's finally block, including failed factories.
/// </summary>
internal sealed class RowCleanupOwner
{
    private readonly TestBuilderContext _context;
    private readonly TestContextEvents _cleanup;
    private int _claimedOrReleased;

    private RowCleanupOwner(TestBuilderContext context, TestContextEvents events)
    {
        _context = context with { };
        if (context.HasStateBag)
            _context.StateBag = new ConcurrentDictionary<string, object?>(context.StateBag);
        _cleanup = new TestContextEvents
        {
            OnDispose = events.OnDispose?.Clone(),
            DataRowScopes = events.DataRowScopes?.Clone()
        };
    }

    internal static RowCleanupOwner? Capture(TestBuilderContext context)
        => context.EventsIfCreated is { } events && (events.OnDispose is not null || events.DataRowScopes is not null)
            ? new(context, events)
            : null;

    internal void ClaimTo(TestBuilderContext target, bool bindState = false)
    {
        if (Interlocked.Exchange(ref _claimedOrReleased, 1) != 0)
            return;

        if (_cleanup.DataRowScopes is { } scopes)
            target.Events.AddDataRowScopes(scopes);
        if (_cleanup.OnDispose is not { } onDispose)
            return;

        if (!bindState)
        {
            (target.Events.OnDispose ??= new()).AddMissingFrom(onDispose);
            return;
        }

        // A discarded method row's callbacks need its state, while metadata and services
        // come from the test that eventually owns cleanup. No lifecycle handlers follow it.
        foreach (var callback in onDispose.InvocationList)
        {
            (target.Events.OnDispose ??= new()).Add(async (_, owner) =>
            {
                var context = new TestContext(owner.TestDetails.TestName, owner.ServiceProvider,
                    owner.ClassContext, _context, owner.CancellationToken);
                context.Metadata.TestDetails = owner.TestDetails;
                try
                {
                    await callback.InvokeAsync(context, context);
                }
                finally
                {
                    context.RemoveFromRegistry();
                    context.Dispose();
                }
            }, callback.Order);
        }
    }

    internal void ClaimForSession()
    {
        if (Interlocked.Exchange(ref _claimedOrReleased, 1) == 0)
            TestSessionContext.DeferOnDisposeToSessionEnd(_cleanup);
    }

    internal async ValueTask<List<Exception>?> ReleaseAsync(
        Func<TestBuilderContext, TestContext> createContext, List<Exception>? exceptions = null)
    {
        if (Interlocked.Exchange(ref _claimedOrReleased, 1) != 0)
            return exceptions;

        if (_cleanup.OnDispose is { } onDispose)
        {
            TestContext? context = null;
            try
            {
                context = createContext(_context);
                foreach (var callback in onDispose.InvocationList)
                {
                    try
                    {
                        await callback.InvokeAsync(context, context).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        (exceptions ??= []).Add(exception);
                    }
                }
            }
            catch (Exception exception)
            {
                (exceptions ??= []).Add(exception);
            }
            finally
            {
                context?.RemoveFromRegistry();
                context?.Dispose();
            }
        }
        if (_cleanup.DataRowScopes is { } scopes)
            exceptions = await scopes.CompleteAsync(exceptions).ConfigureAwait(false);
        return exceptions;
    }
}
