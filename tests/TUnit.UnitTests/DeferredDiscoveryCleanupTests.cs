using Microsoft.Testing.Platform.CommandLine;
using TUnit.Engine.Logging;
using TUnit.Engine.Services;
using TUnit.Engine.Services.TestExecution;

namespace TUnit.UnitTests;

/// <summary>
/// Reflection discovery defers the cleanup of a generic class's inference rows (a dependency injection
/// scope's disposal, for example) to the end of the session that discovered them. In server mode two
/// sessions can run at once, so one session's end must not raise another session's callbacks (#7205).
/// </summary>
public class DeferredDiscoveryCleanupTests
{
    [Test]
    public async Task Deferred_Callbacks_Are_Raised_Once_By_Their_Own_Session()
    {
        var firstCount = 0;
        var secondCount = 0;

        // Creating a session context makes it the current one.
        var first = CreateSession();
        TestOnDisposeRaiser.DeferToSessionEnd(ContextWithOnDispose(() => firstCount++));

        var second = CreateSession();
        TestOnDisposeRaiser.DeferToSessionEnd(ContextWithOnDispose(() => secondCount++));

        var logger = new TUnitFrameworkLogger(null!, null!, null!, new LogLevelProvider(new NoOptions()));

        await TestOnDisposeRaiser.RaiseDeferredForSessionAsync(first, logger);
        await TestOnDisposeRaiser.RaiseDeferredForSessionAsync(first, logger);

        await Assert.That(firstCount).IsEqualTo(1);
        await Assert.That(secondCount).IsEqualTo(0);

        await TestOnDisposeRaiser.RaiseDeferredForSessionAsync(second, logger);

        await Assert.That(firstCount).IsEqualTo(1);
        await Assert.That(secondCount).IsEqualTo(1);
    }

    [Test]
    public async Task Deferring_Detaches_The_Callbacks_From_The_Discovery_Context()
    {
        var count = 0;
        var session = CreateSession();
        var context = ContextWithOnDispose(() => count++);

        TestOnDisposeRaiser.DeferToSessionEnd(context);

        await Assert.That(context.EventsIfCreated?.OnDispose).IsNull();
        await Assert.That(session.DeferredDiscoveryOnDispose?.InvocationList.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Deferring_Events_Moves_Their_Callbacks_To_The_Session()
    {
        // The temporary instance that reads instance method rows injects its properties against bare
        // events, which no test raises.
        var count = 0;
        var session = CreateSession();
        var events = new TestContextEvents();
        events.OnDispose += (_, _) =>
        {
            count++;
            return default;
        };

        TestOnDisposeRaiser.DeferToSessionEnd(events);

        await Assert.That(events.OnDispose).IsNull();
        await Assert.That(session.DeferredDiscoveryOnDispose?.InvocationList.Count).IsEqualTo(1);

        var logger = new TUnitFrameworkLogger(null!, null!, null!, new LogLevelProvider(new NoOptions()));
        await TestOnDisposeRaiser.RaiseDeferredForSessionAsync(session, logger);

        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task Parallel_Discovery_Registrations_Are_All_Drained_Once()
    {
        var session = CreateSession();
        var count = 0;
        await Task.WhenAll(Enumerable.Range(0, 512).Select(_ => Task.Run(() =>
            TestOnDisposeRaiser.DeferToSessionEnd(ContextWithOnDispose(() => Interlocked.Increment(ref count))))));

        var logger = new TUnitFrameworkLogger(null!, null!, null!, new LogLevelProvider(new NoOptions()));
        await Task.WhenAll(
            TestOnDisposeRaiser.RaiseDeferredForSessionAsync(session, logger).AsTask(),
            TestOnDisposeRaiser.RaiseDeferredForSessionAsync(session, logger).AsTask());
        await Assert.That(count).IsEqualTo(512);
        await Assert.That(session.DeferredDiscoveryOnDispose).IsNull();
    }

    [Test]
    public async Task Lent_Constructor_Row_Cleanup_Belongs_To_The_Session()
    {
        var session = CreateSession();
        var count = 0;
        var context = ContextWithOnDispose(() => count++);
        var row = new TUnit.Engine.Building.TestBuilder.UnclaimedClassRow([], null, context);
        row.LendToTemporaryInstance();

        await Assert.That(count).IsEqualTo(0);
        await Assert.That(session.DeferredDiscoveryOnDispose?.InvocationList.Count).IsEqualTo(1);
        var logger = new TUnitFrameworkLogger(null!, null!, null!, new LogLevelProvider(new NoOptions()));
        await TestOnDisposeRaiser.RaiseDeferredForSessionAsync(session, logger);
        await TestOnDisposeRaiser.RaiseDeferredForSessionAsync(session, logger);
        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task Deferred_Data_Row_Scopes_Belong_To_Their_Session()
    {
        var first = CreateSession();
        var firstScope = new CountingScope();
        var firstContext = ContextWithOnDispose(() => { });
        firstContext.Events.GetOrCreateDataRowScopes().Register(firstScope);
        TestOnDisposeRaiser.DeferToSessionEnd(firstContext);
        var second = CreateSession();
        var secondScope = new CountingScope();
        var secondContext = ContextWithOnDispose(() => { });
        secondContext.Events.GetOrCreateDataRowScopes().Register(secondScope);
        TestOnDisposeRaiser.DeferToSessionEnd(secondContext);
        var logger = new TUnitFrameworkLogger(null!, null!, null!, new LogLevelProvider(new NoOptions()));

        await TestOnDisposeRaiser.RaiseDeferredForSessionAsync(first, logger);
        await TestOnDisposeRaiser.RaiseDeferredForSessionAsync(first, logger);
        await Assert.That(firstScope.Count).IsEqualTo(1);
        await Assert.That(secondScope.Count).IsEqualTo(0);
        await Assert.That(firstContext.Events.DataRowScopes).IsNull();
        await TestOnDisposeRaiser.RaiseDeferredForSessionAsync(second, logger);
        await Assert.That(secondScope.Count).IsEqualTo(1);
    }

    private sealed class CountingScope : IAsyncDisposable
    {
        public int Count;
        public ValueTask DisposeAsync()
        {
            Count++;
            return default;
        }
    }

    [Test]
    public async Task Throwing_Session_Callback_Does_Not_Stop_Later_Callbacks()
    {
        var session = CreateSession();
        var context = ContextWithOnDispose(() => throw new InvalidOperationException("cleanup failed"));
        var count = 0;
        context.Events.OnDispose += (_, testContext) =>
        {
            if (testContext is not null) throw new InvalidOperationException("Expected session cleanup without a test");
            count++;
            return default;
        };
        TestOnDisposeRaiser.DeferToSessionEnd(context);
        var callbacks = session.TakeDeferredDiscoveryOnDispose()!;
        var exceptions = new List<Exception>();

        await TestOnDisposeRaiser.RaiseDetachedAsync(session, callbacks.InvocationList, exceptions);

        await Assert.That(count).IsEqualTo(1);
        await Assert.That(exceptions.Count).IsEqualTo(1);
        await Assert.That(exceptions[0].Message).IsEqualTo("cleanup failed");
    }

    [Test]
    public async Task Concurrent_Transfers_Of_The_Same_Events_Are_Drained_Once()
    {
        var session = CreateSession();
        var count = 0;
        var context = ContextWithOnDispose(() => count++);
        await Task.WhenAll(Enumerable.Range(0, 512).Select(_ => Task.Run(() =>
            TestOnDisposeRaiser.DeferToSessionEnd(context))));
        var logger = new TUnitFrameworkLogger(null!, null!, null!, new LogLevelProvider(new NoOptions()));

        await TestOnDisposeRaiser.RaiseDeferredForSessionAsync(session, logger);

        await Assert.That(count).IsEqualTo(1);
    }

    private static TestSessionContext CreateSession()
    {
        var beforeDiscoveryContext = new BeforeTestDiscoveryContext { TestFilter = null };
        var discoveryContext = new TestDiscoveryContext(beforeDiscoveryContext) { TestFilter = null };
        return new TestSessionContext(discoveryContext)
        {
            Id = Guid.NewGuid().ToString(),
            TestFilter = null
        };
    }

    private static TestBuilderContext ContextWithOnDispose(Action onDispose)
    {
        var context = new TestBuilderContext { TestMetadata = TestSessionContext.GlobalStaticPropertyContext.TestMetadata };
        context.Events.OnDispose += (_, _) =>
        {
            onDispose();
            return default;
        };
        return context;
    }

    private sealed class NoOptions : ICommandLineOptions
    {
        public bool IsOptionSet(string optionName) => false;

        public bool TryGetOptionArgumentList(string optionName, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string[]? arguments)
        {
            arguments = null;
            return false;
        }
    }
}
