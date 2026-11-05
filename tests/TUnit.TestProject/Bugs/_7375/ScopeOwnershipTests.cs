using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7375;

public sealed class UntrackingScope : IDisposable
{
    public static ConcurrentBag<UntrackingScope> Created { get; } = [];
    public int DisposeCount { get; private set; }
    public UntrackedService Service { get; } = new();

    public UntrackingScope() => Created.Add(this);

    public void Dispose() => DisposeCount++;
}

public sealed class UntrackedService : IDisposable
{
    public int DisposeCount { get; private set; }
    public void Dispose() => DisposeCount++;
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
