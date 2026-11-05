using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7375;

public sealed class UntrackingScope : IDisposable
{
    public static ConcurrentBag<UntrackingScope> Created { get; } = [];
    public int DisposeCount { get; private set; }
    public UntrackedService Service { get; } = new();

    public UntrackingScope(bool record = true)
    {
        if (record) Created.Add(this);
    }

    public void Dispose() => DisposeCount++;
}

public sealed class UntrackedService : IDisposable, TUnit.Core.Interfaces.IAsyncInitializer
{
    public int DisposeCount { get; private set; }
    public void Dispose() => DisposeCount++;
    public Task InitializeAsync() => Task.CompletedTask;
}

public sealed class UntrackingScopeAttribute : DependencyInjectionDataSourceAttribute<UntrackingScope>
{
    public override UntrackingScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata) => new();
    public override object Create(UntrackingScope scope, Type type) => scope.Service;
    protected override bool ScopeOwnsCreatedObjects(UntrackingScope scope) => false;
}

[EngineTest(ExpectedResult.Pass)]
[UntrackingScope]
public class ScopeOwnershipTests(UntrackedService service)
{
    [Test]
    public async Task Service_Is_Alive() => await Assert.That(service.DisposeCount).IsEqualTo(0);
}

[EngineTest(ExpectedResult.Pass)]
public class ScopeOwnershipObserverTests
{
    [Test]
    [DependsOn(typeof(ScopeOwnershipTests))]
    public async Task Tracker_Disposes_Service_And_Scope_Once()
    {
        await Assert.That(UntrackingScope.Created.Count).IsEqualTo(1);
        var scope = UntrackingScope.Created.Single();
        await Assert.That(scope.DisposeCount).IsEqualTo(1);
        await Assert.That(scope.Service.DisposeCount).IsEqualTo(1);
    }
}

public sealed class PartiallyFailingScopeAttribute : DependencyInjectionDataSourceAttribute<UntrackingScope>
{
    public static ConcurrentBag<UntrackingScope> Created { get; } = [];

    public override UntrackingScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var scope = new UntrackingScope(record: false);
        Created.Add(scope);
        return scope;
    }

    protected override bool ScopeOwnsCreatedObjects(UntrackingScope scope) => false;
    public override object Create(UntrackingScope scope, Type type)
        => type == typeof(UntrackedService) ? scope.Service : throw new InvalidOperationException("Missing registration");
}

[EngineTest(ExpectedResult.Failure)]
public class PartiallyFailedResolutionTests
{
    [Test]
    [PartiallyFailingScope]
    public void Run(UntrackedService service, string missing) => throw new Exception("Should never run");
}

// Run with the failing test: it deliberately cannot join the EngineTest=Pass sweep.
public class PartiallyFailedResolutionObserverTests
{
    [Test]
    [DependsOn(typeof(PartiallyFailedResolutionTests), ProceedOnFailure = true)]
    public async Task Earlier_Service_And_Scope_Are_Disposed_Once()
    {
        await Assert.That(PartiallyFailingScopeAttribute.Created.Count).IsEqualTo(1);
        var scope = PartiallyFailingScopeAttribute.Created.Single();
        await Assert.That(scope.DisposeCount).IsEqualTo(1);
        await Assert.That(scope.Service.DisposeCount).IsEqualTo(1);
    }
}

public sealed class SelfResolvingScopeAttribute : DependencyInjectionDataSourceAttribute<UntrackingScope>
{
    public static ConcurrentBag<UntrackingScope> Created { get; } = [];

    public override UntrackingScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var scope = new UntrackingScope(record: false);
        Created.Add(scope);
        return scope;
    }

    protected override bool ScopeOwnsCreatedObjects(UntrackingScope scope) => false;
    public override object Create(UntrackingScope scope, Type type)
        => type == typeof(UntrackingScope) ? scope : scope.Service;
}

[EngineTest(ExpectedResult.Pass)]
public class SelfResolvingScopeTests
{
    [Test]
    [SelfResolvingScope]
    public async Task Scope_Is_Alive(UntrackingScope scope, UntrackedService service)
    {
        await Assert.That(scope.DisposeCount).IsEqualTo(0);
        await Assert.That(service.DisposeCount).IsEqualTo(0);
    }
}

[EngineTest(ExpectedResult.Pass)]
public class SelfResolvingScopeObserverTests
{
    [Test]
    [DependsOn(typeof(SelfResolvingScopeTests))]
    public async Task Scope_Is_Disposed_Once()
    {
        await Assert.That(SelfResolvingScopeAttribute.Created.Count).IsEqualTo(1);
        var scope = SelfResolvingScopeAttribute.Created.Single();
        await Assert.That(scope.DisposeCount).IsEqualTo(1);
        await Assert.That(scope.Service.DisposeCount).IsEqualTo(1);
    }
}

public sealed class NonDisposableScope
{
    public UntrackedService Service { get; } = new();
}

public sealed class NonDisposableSelfResolvingScopeAttribute : DependencyInjectionDataSourceAttribute<NonDisposableScope>
{
    public static ConcurrentBag<NonDisposableScope> Created { get; } = [];

    public override NonDisposableScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var scope = new NonDisposableScope();
        Created.Add(scope);
        return scope;
    }

    public override object Create(NonDisposableScope scope, Type type) => scope;
}

[EngineTest(ExpectedResult.Pass)]
public class NonDisposableSelfResolvingScopeTests
{
    [Test]
    [NonDisposableSelfResolvingScope]
    public async Task Initializer_Is_Alive(NonDisposableScope scope)
        => await Assert.That(scope.Service.DisposeCount).IsEqualTo(0);
}

[EngineTest(ExpectedResult.Pass)]
public class NonDisposableSelfResolvingScopeObserverTests
{
    [Test]
    [DependsOn(typeof(NonDisposableSelfResolvingScopeTests))]
    public async Task Initializer_Is_Disposed_Once()
    {
        await Assert.That(NonDisposableSelfResolvingScopeAttribute.Created.Count).IsEqualTo(1);
        await Assert.That(NonDisposableSelfResolvingScopeAttribute.Created.Single().Service.DisposeCount).IsEqualTo(1);
    }
}
