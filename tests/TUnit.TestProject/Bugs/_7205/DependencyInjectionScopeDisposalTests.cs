using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7205;

// DependencyInjectionDataSourceAttribute<TScope> disposes each scope it creates through an OnDispose
// handler that it subscribes on TestBuilderContext.Current.Events while its row is fetched. Those
// events never reached the test built from the row, so no scope was ever disposed. Every scope must be
// disposed when its test ends, in both engine modes, including the scope of the class row's first
// fetch that the first test takes (#7205). Each scope owns and disposes the services it resolves, so
// the object tracker must not dispose them a second time.

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

    public List<ProbeService> Services { get; } = [];

    public ProbeService Resolve()
    {
        var service = new ProbeService(this);
        Services.Add(service);
        return service;
    }

    public void Dispose()
    {
        IsDisposed = true;

        foreach (var service in Services)
        {
            service.Dispose();
        }
    }
}

// Like a container-owned service whose cleanup is not repeatable.
public sealed class ProbeService(ProbeScope scope) : IDisposable
{
    private int _disposeCount;

    public ProbeScope Scope { get; } = scope;

    public int DisposeCount => _disposeCount;

    public void Dispose() => Interlocked.Increment(ref _disposeCount);
}

public abstract class ProbeDependencyInjectionAttribute(string owner) : DependencyInjectionDataSourceAttribute<ProbeScope>
{
    public override ProbeScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata) => new(owner);

    public override object? Create(ProbeScope scope, Type type) => scope.Resolve();
}

public sealed class ClassRowDependencyInjectionAttribute() : ProbeDependencyInjectionAttribute(nameof(ClassRowScopeTests));

public sealed class MethodRowsDependencyInjectionAttribute() : ProbeDependencyInjectionAttribute(nameof(ClassRowScopeWithMethodRowsTests));

public sealed class InstanceSourceDependencyInjectionAttribute() : ProbeDependencyInjectionAttribute(nameof(ClassRowScopeWithInstanceMethodDataSourceTests));

public sealed class MethodParameterDependencyInjectionAttribute() : ProbeDependencyInjectionAttribute(nameof(MethodParameterScopeTests));

public sealed class RetriedDependencyInjectionAttribute() : ProbeDependencyInjectionAttribute(nameof(RetriedScopeTests));

public sealed class SkippedDependencyInjectionAttribute() : ProbeDependencyInjectionAttribute(nameof(SkippedScopeTests));

public sealed class SkippedIfEmptyDependencyInjectionAttribute() : ProbeDependencyInjectionAttribute(nameof(SkippedIfEmptyScopeTests));

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

// Every attempt reuses the row's objects, so the scope must outlive a failed attempt and be disposed
// once, after the last one.
[EngineTest(ExpectedResult.Pass)]
[RetriedDependencyInjection]
public class RetriedScopeTests(ProbeService service)
{
    private static int _attempts;

    [Test]
    [Retry(1)]
    public async Task Run()
    {
        await Assert.That(service.Scope.IsDisposed).IsFalse();

        if (Interlocked.Increment(ref _attempts) == 1)
        {
            throw new InvalidOperationException("The first attempt fails, so the test is retried.");
        }
    }
}

// A skipped test never runs, but its class row was still fetched, so its scope must still be disposed.
[EngineTest(ExpectedResult.Pass)]
[SkippedDependencyInjection]
public class SkippedScopeTests(ProbeService service)
{
    [Test]
    [Skip("The scope created for a skipped test is still disposed.")]
    public void Run() => throw new InvalidOperationException($"Should have been skipped, but ran with {service}");
}

// An empty method source with SkipIfEmpty reports a skipped test, which takes the class row's first fetch.
[EngineTest(ExpectedResult.Pass)]
[SkippedIfEmptyDependencyInjection]
public class SkippedIfEmptyScopeTests(ProbeService service)
{
    public static IEnumerable<int> NoRows() => [];

    [Test]
    [MethodDataSource(nameof(NoRows), SkipIfEmpty = true)]
    public void Run(int value) => throw new InvalidOperationException($"Should have been skipped, but ran with {service} and {value}");
}

[EngineTest(ExpectedResult.Pass)]
public class DependencyInjectionScopeDisposalObserverTests
{
    [Test]
    [DependsOn(typeof(ClassRowScopeTests))]
    [DependsOn(typeof(ClassRowScopeWithMethodRowsTests))]
    [DependsOn(typeof(ClassRowScopeWithInstanceMethodDataSourceTests))]
    [DependsOn(typeof(MethodParameterScopeTests))]
    [DependsOn(typeof(RetriedScopeTests))]
    [DependsOn(typeof(SkippedScopeTests), ProceedOnFailure = true)]
    [DependsOn(typeof(SkippedIfEmptyScopeTests), ProceedOnFailure = true)]
    public async Task Every_Scope_Is_Disposed_When_Its_Test_Ends()
    {
        // One scope per test: two tests, then two method rows run twice each.
        await AssertScopes(nameof(ClassRowScopeTests), 2);
        await AssertScopes(nameof(ClassRowScopeWithMethodRowsTests), 4);

        // One scope for the temporary instance that reads the method rows, then one per test.
        await AssertScopes(nameof(ClassRowScopeWithInstanceMethodDataSourceTests), 3);

        // A method parameter scope per test run.
        await AssertScopes(nameof(MethodParameterScopeTests), 2);

        // One scope for both attempts, and one for each skipped test.
        await AssertScopes(nameof(RetriedScopeTests), 1);
        await AssertScopes(nameof(SkippedScopeTests), 1);
        await AssertScopes(nameof(SkippedIfEmptyScopeTests), 1);
    }

    private static async Task AssertScopes(string owner, int expectedCount)
    {
        var scopes = ProbeScope.Created.Where(x => x.Owner == owner).ToArray();

        await Assert.That(scopes.Length).IsEqualTo(expectedCount);
        await Assert.That(scopes.Count(x => x.IsDisposed)).IsEqualTo(expectedCount);

        // Only the scope disposes its services: once each, never again by the object tracker.
        var services = scopes.SelectMany(x => x.Services).ToArray();
        await Assert.That(services.Length).IsEqualTo(expectedCount);
        await Assert.That(services.Count(x => x.DisposeCount == 1)).IsEqualTo(expectedCount);
    }
}
