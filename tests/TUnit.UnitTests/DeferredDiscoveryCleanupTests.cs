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
