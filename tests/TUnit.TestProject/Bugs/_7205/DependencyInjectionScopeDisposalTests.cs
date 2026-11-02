using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7205;

// DependencyInjectionDataSourceAttribute<TScope> disposes each scope it creates through an OnDispose
// handler that it subscribes on TestBuilderContext.Current.Events while its row is fetched. Those
// events never reached the test built from the row, so no scope was ever disposed. Every scope must be
// disposed once, when its test ends, in both engine modes: including the scope of the class row's first
// fetch that the first test takes, a retried test's scope (which every attempt reuses), and the scopes
// of tests that never run (skipped, skipped because a dependency failed, or removed by circular-dependency
// detection) (#7205, #7381).

public sealed class ProbeScope : IDisposable
{
    public ProbeScope(string owner)
    {
        Owner = owner;
        Created.Add(this);
    }

    public static ConcurrentBag<ProbeScope> Created { get; } = [];

    public string Owner { get; }

    private int _disposeCount;

    public bool IsDisposed => _disposeCount > 0;

    public int DisposeCount => _disposeCount;

    public List<ProbeService> Services { get; } = [];

    public ProbeService Resolve()
    {
        var service = new ProbeService(this);
        Services.Add(service);
        return service;
    }

    public void Dispose()
    {
        Interlocked.Increment(ref _disposeCount);

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

public sealed class DependencySkippedDependencyInjectionAttribute() : ProbeDependencyInjectionAttribute(nameof(DependencySkippedScopeTests));

public sealed class CircularDependencyDependencyInjectionAttribute() : ProbeDependencyInjectionAttribute(nameof(CircularDependencyScopeTests));

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
// fetch, so the first test tracks that fetch's objects without receiving them. Each test's own scope
// must be disposed. The temporary instance's scope is disposed at session end by #7383.
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

// A test skipped because its dependency failed never reaches TestCoordinator. The scope created for
// it must still be disposed.
[EngineTest(ExpectedResult.Failure)]
public class DependencySkippedScopeTests
{
    [Test]
    public void Fails() => throw new InvalidOperationException("Fails by design, so the test that depends on it is skipped.");

    [Test]
    [DependsOn(nameof(Fails))]
    [DependencySkippedDependencyInjection]
    public void Run(ProbeService service) => throw new InvalidOperationException($"Should have been skipped, but ran with {service}");
}

// Tests in a circular dependency fail and are removed before execution, so they never reach
// TestCoordinator either. Their scopes must still be disposed.
[EngineTest(ExpectedResult.Failure)]
[UnconditionalSuppressMessage("Usage", "TUnit0033:Conflicting DependsOn attributes")]
public class CircularDependencyScopeTests
{
    [Test]
    [DependsOn(nameof(Second))]
    [CircularDependencyDependencyInjection]
    public void First(ProbeService service) => throw new InvalidOperationException($"Should have failed its circular dependency, but ran with {service}");

    [Test]
    [DependsOn(nameof(First))]
    [CircularDependencyDependencyInjection]
    public void Second(ProbeService service) => throw new InvalidOperationException($"Should have failed its circular dependency, but ran with {service}");
}

// Not tagged [EngineTest]: it depends on DependencySkippedScopeTests, whose Fails test fails by design,
// and asserts the scopes of CircularDependencyScopeTests, so it only holds when the whole _7205 folder
// runs together. Issue7205Tests runs it that way; the [EngineTest=Pass] sweep must not pull it in.
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
    [DependsOn(typeof(DependencySkippedScopeTests), ProceedOnFailure = true)]
    public async Task Every_Scope_Is_Disposed_When_Its_Test_Ends()
    {
        // One scope per test: two tests, then two method rows run twice each.
        await AssertScopes(nameof(ClassRowScopeTests), 2);
        await AssertScopes(nameof(ClassRowScopeWithMethodRowsTests), 4);

        // One scope for the temporary instance that reads the method rows, then one per test. Only the
        // tests' scopes are disposed when they end; the temporary instance's scope is left to #7383.
        await AssertScopes(nameof(ClassRowScopeWithInstanceMethodDataSourceTests), 3, expectedDisposed: 2);

        // A method parameter scope per test run.
        await AssertScopes(nameof(MethodParameterScopeTests), 2);

        // One scope for both attempts, and one for each skipped test.
        await AssertScopes(nameof(RetriedScopeTests), 1);
        await AssertScopes(nameof(SkippedScopeTests), 1);
        await AssertScopes(nameof(SkippedIfEmptyScopeTests), 1);

        // The scope of the test skipped because its dependency failed.
        await AssertScopes(nameof(DependencySkippedScopeTests), 1);

        // One scope for each test removed by circular-dependency detection. Depending on those tests would
        // put this test in their cycle, but the scheduler removes them and disposes their scopes before any
        // test runs.
        await AssertScopes(nameof(CircularDependencyScopeTests), 2);
    }

    private static async Task AssertScopes(string owner, int expectedCount, int? expectedDisposed = null)
    {
        var scopes = ProbeScope.Created.Where(x => x.Owner == owner).ToArray();

        await Assert.That(scopes.Length).IsEqualTo(expectedCount);

        // Disposed once each: a test raises its OnDispose once, not once per attempt or per raising path.
        await Assert.That(scopes.Count(x => x.DisposeCount == 1)).IsEqualTo(expectedDisposed ?? expectedCount);
        await Assert.That(scopes.Count(x => x.DisposeCount > 1)).IsEqualTo(0);
    }
}
