using TUnit.Core;

namespace TUnit.UnitTests;

public class DataRowScopeCollectionTests
{
    [Test]
    public async Task ReleaseUsesReferenceIdentityAndLeavesUnrelatedScopesAlive()
    {
        var scopes = new DataRowScopeCollection();
        var first = new Scope();
        var second = new Scope();
        var firstValue = new EqualValue();
        scopes.RegisterObject(scopes.Register(first), firstValue);
        scopes.RegisterObject(scopes.Register(second), new EqualValue());

        await scopes.ReleaseAsync(firstValue);
        await Assert.That(first.DisposeCount).IsEqualTo(1);
        await Assert.That(second.DisposeCount).IsEqualTo(0);
        await scopes.CompleteAsync();
        await Assert.That(first.DisposeCount).IsEqualTo(1);
        await Assert.That(second.DisposeCount).IsEqualTo(1);
    }

    [Test]
    public async Task SharedResolvedObjectReleasesAllOwnersOnceAcrossClones()
    {
        var scopes = new DataRowScopeCollection();
        var first = new Scope();
        var second = new Scope();
        var value = new object();
        scopes.RegisterObject(scopes.Register(first), value);
        scopes.RegisterObject(scopes.Register(second), value);
        var clone = scopes.Clone();

        await clone.ReleaseAsync(value);
        await scopes.CompleteAsync();
        await clone.CompleteAsync();
        await Assert.That(first.DisposeCount).IsEqualTo(1);
        await Assert.That(second.DisposeCount).IsEqualTo(1);
    }

    [Test]
    public async Task ThrowingScopeDoesNotPreventOtherOwnersFromCompleting()
    {
        var scopes = new DataRowScopeCollection();
        var failed = new Scope { ThrowOnDispose = true };
        var successful = new Scope();
        scopes.Register(failed); // A factory may fail before returning an object.
        scopes.Register(successful);

        var errors = await scopes.CompleteAsync();
        await Assert.That(errors!.Count).IsEqualTo(1);
        await Assert.That(errors[0].Message).IsEqualTo("Scope failed");
        await Assert.That(failed.DisposeCount).IsEqualTo(1);
        await Assert.That(successful.DisposeCount).IsEqualTo(1);
        await Assert.That(await scopes.CompleteAsync()).IsNull();
    }

    [Test]
    public async Task ParallelDiFactoriesKeepEveryScopeAndObjectAcrossClones()
    {
        var context = new TestBuilderContext { TestMetadata = TestContext.Current!.TestDetails.MethodMetadata };
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sources = Enumerable.Range(0, 128).Select(_ => new ConcurrentSource()).ToArray();
        var tasks = sources.Select(source => Task.Run(async () =>
        {
            await ready.Task;
            return source.Resolve(context);
        })).ToArray();
        ready.SetResult();
        var services = await Task.WhenAll(tasks);
        var scopes = context.Events.DataRowScopes!;
        var clone = scopes.Clone();

        // Object lookup must retain every registration, not just the final completion sweep.
        await Task.WhenAll(services.Select(async service => await clone.ReleaseAsync(service)));
        foreach (var source in sources)
            await Assert.That(source.Scope.DisposeCount).IsEqualTo(1);
        await scopes.CompleteAsync();
        await clone.CompleteAsync();
        foreach (var source in sources)
            await Assert.That(source.Scope.DisposeCount).IsEqualTo(1);
    }

    [Test]
    public async Task ParallelCleanupMergesRetainEveryHandlerOnce()
    {
        var destination = new TestContextEvents();
        var calls = new int[128];
        var sources = Enumerable.Range(0, calls.Length).Select(index =>
        {
            var source = new TestContextEvents { OnDispose = new() };
            source.OnDispose.Add((_, _) =>
            {
                Interlocked.Increment(ref calls[index]);
                return default;
            }, index);
            return source;
        }).ToArray();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var merges = sources.Select(source => Task.Run(async () =>
        {
            await ready.Task;
            destination.AddCleanupFrom(source);
            destination.AddCleanupFrom(source);
        })).ToArray();
        ready.SetResult();
        await Task.WhenAll(merges);

        await Assert.That(destination.OnDispose!.InvocationList.Count).IsEqualTo(calls.Length);
        foreach (var handler in destination.OnDispose.InvocationList)
            await handler.InvokeAsync(destination, TestContext.Current!);
        foreach (var count in calls)
            await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task ConcurrentEventSubscriptionsAndMergesPreserveDuplicatesAndSnapshots()
    {
        var destination = new TestContextEvents { OnDispose = new() };
        var source = new TestContextEvents { OnDispose = new() };
        var calls = 0;
        Func<object, TestContext, ValueTask> callback = (_, _) =>
        {
            Interlocked.Increment(ref calls);
            return default;
        };
        source.OnDispose.Add(callback);
        source.OnDispose.Add(callback);
        var originalSnapshot = destination.OnDispose.InvocationList;
        await Task.WhenAll(Enumerable.Range(0, 128).Select(i => Task.Run(() =>
        {
            if (i % 2 == 0)
                destination.OnDispose.Add(callback);
            else
                destination.AddCleanupFrom(source);
        })));
        await Assert.That(originalSnapshot.Count).IsEqualTo(0);
        await Assert.That(destination.OnDispose.InvocationList.Count).IsEqualTo(66);
        var clone = destination.OnDispose.Clone();
        destination.OnDispose.Add(callback);
        await Assert.That(clone.InvocationList.Count).IsEqualTo(66);
        foreach (var invocation in destination.OnDispose.InvocationList)
            await invocation.InvokeAsync(destination, TestContext.Current!);
        await Assert.That(calls).IsEqualTo(67);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task FailedDiResolutionRetainsEarlierServicesForCleanup(bool ownsServices)
    {
        var context = new TestBuilderContext { TestMetadata = TestContext.Current!.TestDetails.MethodMetadata };
        var source = new FailingSource(ownsServices);
        try
        {
            source.Resolve(context);
            throw new Exception("Expected resolution to fail");
        }
        catch (InvalidOperationException exception)
        {
            await Assert.That(exception.Message).IsEqualTo("Resolution failed");
        }

        var scopes = context.Events.DataRowScopes!;
        var clone = scopes.Clone();
        await Assert.That(source.Service.DisposeCount).IsEqualTo(0);
        await scopes.CompleteAsync();
        await clone.CompleteAsync();
        await Assert.That(source.Scope.DisposeCount).IsEqualTo(1);
        // Owning scopes retain responsibility; non-owning scopes hand cleanup to TUnit.
        await Assert.That(source.Service.DisposeCount).IsEqualTo(ownsServices ? 0 : 1);
    }

    private sealed class AsyncService : IAsyncDisposable
    {
        internal int DisposeCount;
        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            DisposeCount++;
        }
    }

    private sealed class FailingSource(bool ownsServices) : DependencyInjectionDataSourceAttribute<Scope>
    {
        internal Scope Scope { get; } = new();
        internal AsyncService Service { get; } = new();
        private int _calls;

        internal void Resolve(TestBuilderContext context)
        {
            var metadata = new DataGeneratorMetadata
            {
                TestBuilderContext = new TestBuilderContextAccessor(context),
                MembersToGenerate = [context.TestMetadata.Class, context.TestMetadata.Class, context.TestMetadata.Class],
                TestInformation = null,
                Type = TUnit.Core.Enums.DataGeneratorType.Property,
                TestSessionId = "failed-scope-registration",
                TestClassInstance = null,
                ClassInstanceArguments = []
            };
            GenerateDataSources(metadata).Single()();
        }

        public override Scope CreateScope(DataGeneratorMetadata metadata) => Scope;
        protected override bool ScopeOwnsCreatedObjects(Scope scope) => ownsServices;
        public override object Create(Scope scope, Type type)
            => ++_calls < 3 ? Service : throw new InvalidOperationException("Resolution failed");
    }

    private sealed class ConcurrentSource : DependencyInjectionDataSourceAttribute<Scope>
    {
        internal Scope Scope { get; } = new();
        internal object Resolve(TestBuilderContext context)
        {
            var metadata = new DataGeneratorMetadata
            {
                TestBuilderContext = new TestBuilderContextAccessor(context),
                MembersToGenerate = [context.TestMetadata.Class],
                TestInformation = null,
                Type = TUnit.Core.Enums.DataGeneratorType.Property,
                TestSessionId = "concurrent-scope-registration",
                TestClassInstance = null,
                ClassInstanceArguments = []
            };
            return GenerateDataSources(metadata).Single()()![0]!;
        }
        public override Scope CreateScope(DataGeneratorMetadata metadata) => Scope;
        public override object Create(Scope scope, Type type) => new object();
    }

    private sealed class Scope : IDisposable
    {
        public int DisposeCount;
        public bool ThrowOnDispose;

        public void Dispose()
        {
            DisposeCount++;
            if (ThrowOnDispose)
            {
                throw new InvalidOperationException("Scope failed");
            }
        }
    }

    private sealed record EqualValue;
}
