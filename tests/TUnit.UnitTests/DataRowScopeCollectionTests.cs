using TUnit.Core;
using TUnit.Core.Discovery;

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

    [Test]
    public async Task FailedRowsReleaseSharedServicesOnlyAfterTheFinalOwner()
    {
        var service = new AsyncService();
        // Model a successful test holding the same object through the tracker.
        var tracker = new TUnit.Core.Tracking.ObjectTracker(new(), GlobalContext.Current.Disposer);
        tracker.TrackStaticLeaseObject(service);
        var first = new TestBuilderContext { TestMetadata = TestContext.Current!.TestDetails.MethodMetadata };
        var second = new TestBuilderContext { TestMetadata = first.TestMetadata };
        foreach (var context in new[] { first, second })
        {
            await Assert.That(() => new FailingSource(false, service).Resolve(context))
                .Throws<InvalidOperationException>();
        }
        var clone = first.Events.DataRowScopes!.Clone();
        await first.Events.DataRowScopes.CompleteAsync();
        await clone.CompleteAsync();
        await Assert.That(service.DisposeCount).IsEqualTo(0);
        await tracker.ReleaseStaticLeaseObjectAsync(service);
        await Assert.That(service.DisposeCount).IsEqualTo(0);
        await second.Events.DataRowScopes!.CompleteAsync();
        await Assert.That(service.DisposeCount).IsEqualTo(1);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task FailedRowsRetainNestedInitializersAcrossSharedNonDisposableRoots(bool disposableRoot)
    {
        var nested = new NestedInitializer();
        object root = disposableRoot ? new DisposableRoot(nested) : new InitializerRoot(nested);
        var tracker = new TUnit.Core.Tracking.ObjectTracker(new(), GlobalContext.Current.Disposer);
        tracker.TrackStaticLeaseObject(nested);
        var contexts = Enumerable.Range(0, 2).Select(_ => new TestBuilderContext
        {
            TestMetadata = TestContext.Current!.TestDetails.MethodMetadata
        }).ToArray();
        foreach (var context in contexts)
            await Assert.That(() => new FailingSource(false, root).Resolve(context)).Throws<InvalidOperationException>();

        await contexts[0].Events.DataRowScopes!.CompleteAsync();
        await tracker.ReleaseStaticLeaseObjectAsync(nested);
        await Assert.That(nested.DisposeCount).IsEqualTo(0);
        await contexts[1].Events.DataRowScopes!.CompleteAsync();
        await contexts[1].Events.DataRowScopes!.Clone().CompleteAsync();
        await Assert.That(nested.DisposeCount).IsEqualTo(1);
        await Assert.That(nested.InitializeCount).IsEqualTo(0);
        if (root is DisposableRoot disposable)
            await Assert.That(disposable.DisposeCount).IsEqualTo(1);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task FailedRowRefreshesExternalOwnershipBeforeCleanup(bool throwingGetter)
    {
        var root = new ChangingInitializerRoot();
        var external = new DataRowScopeCollection();
        var owner = external.Register(new Scope());
        external.RegisterObject(owner, root);
        external.CaptureOwnership();
        root.Nested = new NestedInitializer();
        root.Throw = throwingGetter;
        var context = new TestBuilderContext { TestMetadata = TestContext.Current!.TestDetails.MethodMetadata };
        await Assert.That(() => new FailingSource(false, root).Resolve(context))
            .Throws<InvalidOperationException>().WithMessage("Resolution failed");
        await context.Events.DataRowScopes!.CompleteAsync();
        await Assert.That(root.Nested.DisposeCount).IsEqualTo(0);
        await Assert.That(ExternalObjectOwnership.IsOwned(root.Nested)).IsTrue();
        await external.CompleteAsync();
        GC.KeepAlive(external);
    }

    private sealed class ChangingInitializerRoot
    {
        public bool Throw;
        public NestedInitializer Broken => Throw ? throw new InvalidOperationException("Getter failed") : Nested;
        public NestedInitializer Nested { get; set; } = new();
    }

    [Test]
    public async Task FailedRowKeepsResolutionErrorAndRootCleanupWhenAnInitializerGetterThrows()
    {
        var root = new ThrowingInitializerRoot();
        var context = new TestBuilderContext { TestMetadata = TestContext.Current!.TestDetails.MethodMetadata };
        await Assert.That(() => new FailingSource(false, root).Resolve(context))
            .Throws<InvalidOperationException>().WithMessage("Resolution failed");
        await context.Events.DataRowScopes!.CompleteAsync();
        await Assert.That(root.DisposeCount).IsEqualTo(1);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task FailedRowRetainsLaterInitializersAfterAGetterThrows(bool generatedMetadata)
    {
        var root = CreateThrowingRoot(generatedMetadata);
        var context = new TestBuilderContext { TestMetadata = TestContext.Current!.TestDetails.MethodMetadata };
        await Assert.That(() => new FailingSource(false, root).Resolve(context))
            .Throws<InvalidOperationException>().WithMessage("Resolution failed");
        await context.Events.DataRowScopes!.CompleteAsync();
        await Assert.That(root.DisposeCount).IsEqualTo(1);
        await Assert.That(root.Later.DisposeCount).IsEqualTo(1);
        await Assert.That(root.Later.InitializeCount).IsEqualTo(0);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task NormalDiscoveryStillReportsGetterFailures(bool generatedMetadata)
    {
        var root = CreateThrowingRoot(generatedMetadata);
        await Assert.That(() => new ObjectGraphDiscoverer().DiscoverTrackableObjectGraph(root, CancellationToken.None))
            .Throws<DataSourceException>();
    }

    [Test]
    public async Task FailedRowRetainsInitializersAfterAnInjectableGetterThrows()
    {
        var root = new ThrowingInjectableRoot();
        var plan = TUnit.Core.PropertyInjection.PropertyInjectionCache.GetOrCreatePlan(root.GetType());
        if (SourceRegistrar.IsEnabled)
            await Assert.That(plan.SourceGeneratedProperties.Length).IsEqualTo(2);
        else
            await Assert.That(plan.ReflectionProperties.Length).IsEqualTo(2);
        var context = new TestBuilderContext { TestMetadata = TestContext.Current!.TestDetails.MethodMetadata };
        await Assert.That(() => new FailingSource(false, root).Resolve(context))
            .Throws<InvalidOperationException>().WithMessage("Resolution failed");
        await context.Events.DataRowScopes!.CompleteAsync();
        await Assert.That(root.Later.DisposeCount).IsEqualTo(1);
        await Assert.That(root.Later.InitializeCount).IsEqualTo(0);
        await Assert.That(root.InjectedSibling.DisposeCount).IsEqualTo(1);
    }

    [Test]
    public async Task NormalDiscoveryStillReportsInjectableGetterFailures()
    {
        await Assert.That(() => new ObjectGraphDiscoverer().DiscoverTrackableObjectGraph(new ThrowingInjectableRoot(), CancellationToken.None))
            .Throws<Exception>();
    }

    public sealed class ThrowingInjectableRoot
    {
        [ClassDataSource<NestedInitializer>]
        public NestedInitializer Broken { get => throw new InvalidOperationException("Getter failed"); set { } }

        [ClassDataSource<NestedInitializer>]
        public NestedInitializer InjectedSibling { get; set; } = new();

        public NestedInitializer Later { get; } = new();
    }

    private static ThrowingInitializerRoot CreateThrowingRoot(bool generatedMetadata)
    {
        if (!generatedMetadata)
            return new ThrowingInitializerRoot();

        // Exercise the same ordered getter delegates emitted by the source generator,
        // independently of the reflection fallback used by the base type.
        InitializerPropertyRegistry.Register(typeof(RegisteredThrowingRoot),
        [
            new() { PropertyName = "Nested", PropertyType = typeof(NestedInitializer), GetValue = obj => ((RegisteredThrowingRoot)obj).Nested },
            new() { PropertyName = "Later", PropertyType = typeof(NestedInitializer), GetValue = obj => ((RegisteredThrowingRoot)obj).Later }
        ]);
        return new RegisteredThrowingRoot();
    }

    private sealed class RegisteredThrowingRoot : ThrowingInitializerRoot;

    private class ThrowingInitializerRoot : IDisposable
    {
        public NestedInitializer Nested => throw new InvalidOperationException("Getter failed");
        public NestedInitializer Later { get; } = new();
        public int DisposeCount;
        public void Dispose() => DisposeCount++;
    }

    private class InitializerRoot(NestedInitializer nested)
    {
        public NestedInitializer Nested { get; } = nested;
    }

    private sealed class DisposableRoot(NestedInitializer nested) : InitializerRoot(nested), IDisposable
    {
        public int DisposeCount;
        public void Dispose() => DisposeCount++;
    }

    public sealed class NestedInitializer : TUnit.Core.Interfaces.IAsyncInitializer, IAsyncDisposable
    {
        public int DisposeCount;
        public int InitializeCount;
        public Task InitializeAsync() { InitializeCount++; return Task.CompletedTask; }
        public ValueTask DisposeAsync() { DisposeCount++; return default; }
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

    private sealed class FailingSource(bool ownsServices, object? shared = null) : DependencyInjectionDataSourceAttribute<Scope>
    {
        internal Scope Scope { get; } = new();
        internal AsyncService Service { get; } = new();
        private object ResolvedService => shared ?? Service;
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
            => ++_calls < 3 ? ResolvedService : throw new InvalidOperationException("Resolution failed");
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
