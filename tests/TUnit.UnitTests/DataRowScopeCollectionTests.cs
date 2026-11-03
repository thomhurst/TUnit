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
