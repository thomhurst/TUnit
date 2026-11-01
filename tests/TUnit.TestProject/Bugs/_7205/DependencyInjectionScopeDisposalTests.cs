using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7205;

// DependencyInjectionDataSourceAttribute<TScope> disposes each scope it creates through an OnDispose
// handler that it subscribes on TestBuilderContext.Current.Events while its row is fetched. Those
// events never reached the test built from the row, so no scope was ever disposed. Every scope must be
// disposed when its test ends, in both engine modes, including the scope of the class row's first
// fetch that the first test takes (#7205).

public sealed class ProbeScope : IDisposable
{
    public ProbeScope(string owner)
    {
        Owner = owner;
        Created.Add(this);
    }

    public static ConcurrentBag<ProbeScope> Created { get; } = [];

    public string Owner { get; }

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

public sealed class ProbeService(ProbeScope scope)
{
    public ProbeScope Scope { get; } = scope;
}

public abstract class ProbeDependencyInjectionAttribute(string owner) : DependencyInjectionDataSourceAttribute<ProbeScope>
{
    public override ProbeScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata) => new(owner);

    public override object? Create(ProbeScope scope, Type type) => new ProbeService(scope);
}

public sealed class ClassRowDependencyInjectionAttribute() : ProbeDependencyInjectionAttribute(nameof(ClassRowScopeTests));

public sealed class MethodRowsDependencyInjectionAttribute() : ProbeDependencyInjectionAttribute(nameof(ClassRowScopeWithMethodRowsTests));

public sealed class InstanceSourceDependencyInjectionAttribute() : ProbeDependencyInjectionAttribute(nameof(ClassRowScopeWithInstanceMethodDataSourceTests));

public sealed class MethodParameterDependencyInjectionAttribute() : ProbeDependencyInjectionAttribute(nameof(MethodParameterScopeTests));

[EngineTest(ExpectedResult.Pass)]
[ClassRowDependencyInjection]
public class ClassRowScopeTests(ProbeService service)
{
    [Test]
    public async Task One() => await Assert.That(service.Scope.IsDisposed).IsFalse();

    [Test]
    public async Task Two() => await Assert.That(service.Scope.IsDisposed).IsFalse();
}

[EngineTest(ExpectedResult.Pass)]
[MethodRowsDependencyInjection]
public class ClassRowScopeWithMethodRowsTests(ProbeService service)
{
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Repeat(1)]
    public async Task Run(int value) => await Assert.That(service.Scope.IsDisposed).IsFalse();
}

// The temporary instance that reads the instance method rows is built from the class row's first
// fetch, so the first test tracks that fetch's objects without receiving them. Its scope must still
// be disposed.
[EngineTest(ExpectedResult.Pass)]
[InstanceSourceDependencyInjection]
public class ClassRowScopeWithInstanceMethodDataSourceTests(ProbeService service)
{
    public IEnumerable<int> Rows() => [1, 2];

    [Test]
    [InstanceMethodDataSource(nameof(Rows))]
    public async Task Run(int value) => await Assert.That(service.Scope.IsDisposed).IsFalse();
}

[EngineTest(ExpectedResult.Pass)]
public class MethodParameterScopeTests
{
    [Test]
    [MethodParameterDependencyInjection]
    [Repeat(1)]
    public async Task Run(ProbeService service) => await Assert.That(service.Scope.IsDisposed).IsFalse();
}

[EngineTest(ExpectedResult.Pass)]
public class DependencyInjectionScopeDisposalObserverTests
{
    [Test]
    [DependsOn(typeof(ClassRowScopeTests))]
    [DependsOn(typeof(ClassRowScopeWithMethodRowsTests))]
    [DependsOn(typeof(ClassRowScopeWithInstanceMethodDataSourceTests))]
    [DependsOn(typeof(MethodParameterScopeTests))]
    public async Task Every_Scope_Is_Disposed_When_Its_Test_Ends()
    {
        // One scope per test: two tests, then two method rows run twice each.
        await AssertScopes(nameof(ClassRowScopeTests), 2);
        await AssertScopes(nameof(ClassRowScopeWithMethodRowsTests), 4);

        // One scope for the temporary instance that reads the method rows, then one per test.
        await AssertScopes(nameof(ClassRowScopeWithInstanceMethodDataSourceTests), 3);

        // A method parameter scope per test run.
        await AssertScopes(nameof(MethodParameterScopeTests), 2);
    }

    private static async Task AssertScopes(string owner, int expectedCount)
    {
        var scopes = ProbeScope.Created.Where(x => x.Owner == owner).ToArray();

        await Assert.That(scopes.Length).IsEqualTo(expectedCount);
        await Assert.That(scopes.Count(x => x.IsDisposed)).IsEqualTo(expectedCount);
    }
}
