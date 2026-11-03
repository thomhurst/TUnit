using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Engine.Services.TestExecution;

namespace TUnit.UnitTests;

public class StaticScopeCleanupTests
{
    [Test]
    public async Task StaticScopeOwnsServiceUntilSessionCleanup()
    {
        var events = new TestContextEvents();
        var source = new ScopeSource();
        var service = source.Resolve(events);
        var context = new TestBuilderContext { TestMetadata = TestContext.Current!.TestDetails.MethodMetadata, Events = events };

        await Assert.That(service.DisposeCount).IsEqualTo(0);
        await Assert.That(source.Scope.DisposeCount).IsEqualTo(0);

        await TestOnDisposeRaiser.RaiseForSessionAsync(context, []);
        // Session cleanup detaches the collection so repeated cleanup cannot dispose it twice.
        await TestOnDisposeRaiser.RaiseForSessionAsync(context, []);
        await Assert.That(source.Scope.DisposeCount).IsEqualTo(1);
        await Assert.That(service.DisposeCount).IsEqualTo(1);
        await Assert.That(events.DataRowScopes).IsNull();

        var laterSource = new ScopeSource();
        var laterService = laterSource.Resolve(events);
        await TestOnDisposeRaiser.RaiseForSessionAsync(context, []);
        await Assert.That(laterSource.Scope.DisposeCount).IsEqualTo(1);
        await Assert.That(source.Scope.DisposeCount).IsEqualTo(1);
        await Assert.That(events.DataRowScopes).IsNull();
    }

    [Test]
    public async Task FailedStaticFactoryScopeIsReleasedAndOtherScopesStillDispose()
    {
        var events = new TestContextEvents();
        var failed = new ScopeSource { ThrowOnCreate = true, ThrowOnDispose = true };
        try
        {
            failed.Resolve(events);
        }
        catch (InvalidOperationException)
        {
        }
        var successful = new ScopeSource();
        var service = successful.Resolve(events);
        var context = new TestBuilderContext { TestMetadata = TestContext.Current!.TestDetails.MethodMetadata, Events = events };
        var exceptions = new List<Exception>();

        await TestOnDisposeRaiser.RaiseForSessionAsync(context, exceptions);

        await Assert.That(failed.Scope.DisposeCount).IsEqualTo(1);
        await Assert.That(successful.Scope.DisposeCount).IsEqualTo(1);
        await Assert.That(service.DisposeCount).IsEqualTo(1);
        await Assert.That(events.DataRowScopes).IsNull();
        await Assert.That(exceptions.Count).IsEqualTo(1);
        await Assert.That(exceptions[0].Message).IsEqualTo("Scope cleanup failure");
    }

    private sealed class ScopeSource : DependencyInjectionDataSourceAttribute<Scope>
    {
        public Scope Scope { get; } = new();
        public bool ThrowOnCreate;
        public bool ThrowOnDispose;

        public Service Resolve(TestContextEvents events)
        {
            var method = TestContext.Current!.TestDetails.MethodMetadata;
            var metadata = new DataGeneratorMetadata
            {
                TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext
                {
                    TestMetadata = method,
                    Events = events
                }),
                MembersToGenerate = [method.Class],
                TestInformation = null,
                Type = DataGeneratorType.Property,
                TestSessionId = "static-scope-regression",
                TestClassInstance = null,
                ClassInstanceArguments = []
            };
            return (Service) GenerateDataSources(metadata).Single()()![0]!;
        }

        public override Scope CreateScope(DataGeneratorMetadata metadata)
        {
            Scope.ThrowOnDispose = ThrowOnDispose;
            return Scope;
        }

        public override object Create(Scope scope, Type type)
            => ThrowOnCreate ? throw new InvalidOperationException("Factory failure") : scope.Service = new Service();
    }

    private sealed class Scope : IAsyncDisposable
    {
        public int DisposeCount;
        public bool ThrowOnDispose;
        public Service? Service;

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            DisposeCount++;
            Service?.Dispose();
            if (ThrowOnDispose) throw new InvalidOperationException("Scope cleanup failure");
        }
    }

    private sealed class Service : IDisposable
    {
        public int DisposeCount;
        public void Dispose()
        {
            if (++DisposeCount > 1) throw new InvalidOperationException("Service disposed twice");
        }
    }
}
