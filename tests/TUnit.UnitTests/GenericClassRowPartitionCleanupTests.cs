using TUnit.Core.Enums;
using Microsoft.Testing.Platform.CommandLine;
using TUnit.Engine.Logging;
using TUnit.Engine.Services;
using TUnit.Engine.Services.TestExecution;

namespace TUnit.UnitTests;

/// <summary>
/// Source-generated mode reads every row of a partitioned class data source to choose the rows that infer a
/// generic class instantiation, and the first fetch of a chosen row returns those values without calling its
/// factory again. The cleanup a row's source subscribed while it was read (a dependency injection scope's
/// disposal) must not stay on the build's context, where no test raises it. Like the rows reflection
/// discovery reads to infer type arguments, it is raised once at session end (#7205).
/// </summary>
public class GenericClassRowPartitionCleanupTests
{
    [Test]
    public async Task Cleanup_Subscribed_While_Rows_Are_Evaluated_Runs_At_Session_End()
    {
        var disposeCount = 0;
        var session = CreateSession();
        var buildContext = new TestBuilderContext { TestMetadata = TestSessionContext.GlobalStaticPropertyContext.TestMetadata };
        var accessor = new TestBuilderContextAccessor(buildContext);
        var source = new SubscribingSource(() => Interlocked.Increment(ref disposeCount));

        var partitioned = GenericClassRowPartition.Create([typeof(int)], GenericClassRowFallback.None, [source], 0);

        var factories = new List<Func<Task<object?[]?>>>();
        await foreach (var factory in partitioned[0].GetDataRowsAsync(CreateMetadata(accessor)))
        {
            factories.Add(factory);
        }

        // The row is fetched against a context of its own, as the engine fetches a class row.
        var fetchContext = new TestBuilderContext { TestMetadata = buildContext.TestMetadata };
        accessor.Current = fetchContext;
        var row = await factories.Single()();
        accessor.Current = buildContext;

        await Assert.That(row).IsEquivalentTo(new object?[] { 7 });
        await Assert.That(source.FactoryCalls).IsEqualTo(1);

        // Neither the build's context nor the fetch's holds the evaluated row's cleanup.
        await Assert.That(buildContext.EventsIfCreated?.OnDispose).IsNull();
        await Assert.That(fetchContext.EventsIfCreated?.OnDispose).IsNull();
        await Assert.That(session.DeferredDiscoveryOnDispose?.InvocationList.Count).IsEqualTo(1);

        var logger = new TUnitFrameworkLogger(null!, null!, null!, new LogLevelProvider(new NoOptions()));
        await TestOnDisposeRaiser.RaiseDeferredForSessionAsync(session, logger);
        await TestOnDisposeRaiser.RaiseDeferredForSessionAsync(session, logger);

        await Assert.That(disposeCount).IsEqualTo(1);
    }

    [Test]
    public async Task Failed_Partition_Evaluation_Restores_Context_And_Releases_Scope_Without_Tests()
    {
        var disposeCount = 0;
        var session = CreateSession();
        var context = new TestBuilderContext { TestMetadata = TestSessionContext.GlobalStaticPropertyContext.TestMetadata };
        var accessor = new TestBuilderContextAccessor(context);
        var source = new SubscribingSource(() => disposeCount++, failFactory: true);
        var partitioned = GenericClassRowPartition.Create([typeof(int)], GenericClassRowFallback.None, [source], 0);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in partitioned[0].GetDataRowsAsync(CreateMetadata(accessor)))
            {
            }
        });

        await Assert.That(accessor.Current).IsSameReferenceAs(context);
        await Assert.That(disposeCount).IsEqualTo(0);
        var logger = new TUnitFrameworkLogger(null!, null!, null!, new LogLevelProvider(new NoOptions()));
        await TestOnDisposeRaiser.RaiseDeferredForSessionAsync(session, logger);
        await Assert.That(disposeCount).IsEqualTo(1);
    }

    // Like a dependency injection source: each row's factory creates a scope and subscribes its disposal on
    // the context current while the row is fetched.
    private sealed class SubscribingSource(Action onDispose, bool failFactory = false) : IDataSourceAttribute
    {
        private int _factoryCalls;

        public int FactoryCalls => _factoryCalls;

        public bool SkipIfEmpty { get; set; }

        public bool DeferEnumeration { get; set; }

        public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
        {
            await Task.Yield();
            yield return () =>
            {
                Interlocked.Increment(ref _factoryCalls);
                dataGeneratorMetadata.TestBuilderContext.Current.Events.OnDispose += (_, _) =>
                {
                    onDispose();
                    return default;
                };
                return failFactory
                    ? Task.FromException<object?[]?>(new InvalidOperationException("row factory failed"))
                    : Task.FromResult<object?[]?>([7]);
            };
        }
    }

    private static DataGeneratorMetadata CreateMetadata(TestBuilderContextAccessor accessor) => new()
    {
        TestBuilderContext = accessor,
        MembersToGenerate = [],
        TestInformation = null,
        Type = DataGeneratorType.ClassParameters,
        TestSessionId = string.Empty,
        TestClassInstance = null,
        ClassInstanceArguments = null,
    };

    private static TestSessionContext CreateSession()
    {
        // Creating a session context makes it the current one.
        var beforeDiscoveryContext = new BeforeTestDiscoveryContext { TestFilter = null };
        var discoveryContext = new TestDiscoveryContext(beforeDiscoveryContext) { TestFilter = null };
        return new TestSessionContext(discoveryContext)
        {
            Id = Guid.NewGuid().ToString(),
            TestFilter = null
        };
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
