using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7205;

// DependencyInjectionDataSourceAttribute<TScope> disposes each scope it creates through an OnDispose
// handler that it subscribes on TestBuilderContext.Current.Events while its row is fetched. Those
// events never reached the test built from the row, so no scope was ever disposed. Every scope must be
// disposed when its test ends, in both engine modes, including the scope of the class row's first
// fetch that the first test takes (#7205). Each scope owns and disposes the services it resolves, so
// the object tracker must not dispose them a second time. A scope that is not disposable cannot dispose
// what Create returns, so the object tracker still does. A row that fails after its scope was created
// still disposes that scope (#7326), and so do a test skipped because its dependency failed and a
// test removed by circular-dependency detection.

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

public sealed class FailedRowDependencyInjectionAttribute() : ProbeDependencyInjectionAttribute(nameof(FailedRowScopeTests));

public sealed class DependencySkippedDependencyInjectionAttribute() : ProbeDependencyInjectionAttribute(nameof(DependencySkippedScopeTests));

public sealed class CircularDependencyDependencyInjectionAttribute() : ProbeDependencyInjectionAttribute(nameof(CircularDependencyScopeTests));

// A scope that is not disposable, whose Create constructs each service itself: nothing but the object
// tracker can dispose those services.
public sealed class UnownedService : IDisposable
{
    public UnownedService()
    {
        Created.Add(this);
    }

    public static ConcurrentBag<UnownedService> Created { get; } = [];

    private int _disposeCount;

    public int DisposeCount => _disposeCount;

    public void Dispose() => Interlocked.Increment(ref _disposeCount);
}

public sealed class NonDisposableScopeDependencyInjectionAttribute : DependencyInjectionDataSourceAttribute<NonDisposableScopeDependencyInjectionAttribute.Scope>
{
    public sealed class Scope;

    public override Scope CreateScope(DataGeneratorMetadata dataGeneratorMetadata) => new();

    public override object? Create(Scope scope, Type type) => new UnownedService();
}

// A scope that resolves a service holding a scope-owned dependency in an IAsyncInitializer property. The
// object tracker finds the dependency beneath the service, but only the scope may dispose it.
public sealed class NestedProbeScope : IDisposable
{
    public NestedProbeScope()
    {
        Created.Add(this);
    }

    public static ConcurrentBag<NestedProbeScope> Created { get; } = [];

    public List<NestedProbeDependency> Dependencies { get; } = [];

    public NestedProbeService Resolve()
    {
        var dependency = new NestedProbeDependency();
        Dependencies.Add(dependency);
        return new NestedProbeService(dependency);
    }

    public void Dispose()
    {
        foreach (var dependency in Dependencies)
        {
            dependency.Dispose();
        }
    }
}

// Not disposable itself, so nothing but the scope owns what it holds. TUnit injects Fixture, so the
// object tracker owns and disposes it, even though it sits beneath a scope-owned service.
public sealed class NestedProbeService(NestedProbeDependency dependency)
{
    public NestedProbeDependency Dependency { get; } = dependency;

    [ClassDataSource<InjectedProbeFixture>(Shared = SharedType.None)]
    public InjectedProbeFixture Fixture { get; set; } = null!;
}

public sealed class InjectedProbeFixture : IAsyncInitializer, IDisposable
{
    private int _disposeCount;

    public InjectedProbeFixture()
    {
        Created.Add(this);
    }

    public static ConcurrentBag<InjectedProbeFixture> Created { get; } = [];

    public bool IsInitialized { get; private set; }

    public int DisposeCount => _disposeCount;

    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public void Dispose() => Interlocked.Increment(ref _disposeCount);
}

public sealed class NestedProbeDependency : IAsyncInitializer, IDisposable
{
    private int _disposeCount;

    public bool IsInitialized { get; private set; }

    public int DisposeCount => _disposeCount;

    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public void Dispose() => Interlocked.Increment(ref _disposeCount);
}

public sealed class NestedDependencyInjectionAttribute : DependencyInjectionDataSourceAttribute<NestedProbeScope>
{
    public override NestedProbeScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata) => new();

    public override object? Create(NestedProbeScope scope, Type type) => scope.Resolve();
}

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

// The temporary instance for the instance method rows cannot be constructed, so the class row's first
// fetch ends in a failed test instead. That test must still dispose the fetch's scope.
[EngineTest(ExpectedResult.Failure)]
[FailedRowDependencyInjection]
public class FailedRowScopeTests
{
    public FailedRowScopeTests(ProbeService service)
    {
        throw new InvalidOperationException($"The class cannot be constructed with {service}, so its row fails.");
    }

    public IEnumerable<int> Rows() => [1];

    [Test]
    [InstanceMethodDataSource(nameof(Rows))]
    public void Run(int value) => throw new InvalidOperationException($"Should have failed to build, but ran with {value}");
}

// A test skipped because its dependency failed never reaches TestCoordinator. The scope created for
// it must still be disposed, because the object tracker no longer disposes the services the scope owns.
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

[EngineTest(ExpectedResult.Pass)]
[NonDisposableScopeDependencyInjection]
public class NonDisposableScopeTests(UnownedService service)
{
    [Test]
    public async Task Run() => await Assert.That(service.DisposeCount).IsEqualTo(0);
}

[EngineTest(ExpectedResult.Pass)]
[NestedDependencyInjection]
public class NestedScopeDependencyTests(NestedProbeService service)
{
    [Test]
    public async Task Run()
    {
        // The dependency is still found beneath the service and initialized.
        await Assert.That(service.Dependency.IsInitialized).IsTrue();
        await Assert.That(service.Dependency.DisposeCount).IsEqualTo(0);
        await Assert.That(service.Fixture.IsInitialized).IsTrue();
        await Assert.That(service.Fixture.DisposeCount).IsEqualTo(0);
    }
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
    [DependsOn(typeof(FailedRowScopeTests), ProceedOnFailure = true)]
    [DependsOn(typeof(DependencySkippedScopeTests), ProceedOnFailure = true)]
    [DependsOn(typeof(NonDisposableScopeTests))]
    [DependsOn(typeof(NestedScopeDependencyTests))]
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

        // The failed row's scope: only the temporary instance's fetch, which failed to construct.
        await AssertScopes(nameof(FailedRowScopeTests), 1);

        // The scope of the test skipped because its dependency failed.
        await AssertScopes(nameof(DependencySkippedScopeTests), 1);

        // One scope for each test removed by circular-dependency detection. Depending on those tests would
        // put this test in their cycle, but the scheduler removes them and disposes their scopes before any
        // test runs.
        await AssertScopes(nameof(CircularDependencyScopeTests), 2);

        // No scope owns these, so the object tracker disposes each once.
        await Assert.That(UnownedService.Created.Count).IsEqualTo(1);
        await Assert.That(UnownedService.Created.Count(x => x.DisposeCount == 1)).IsEqualTo(1);

        // The scope disposes the dependency it created beneath the service; the object tracker does not.
        var nestedDependencies = NestedProbeScope.Created.SelectMany(x => x.Dependencies).ToArray();
        await Assert.That(nestedDependencies.Length).IsEqualTo(1);
        await Assert.That(nestedDependencies.Count(x => x.DisposeCount == 1)).IsEqualTo(1);

        // TUnit injected the fixture beneath the scope-owned service, so the object tracker disposes it once.
        await Assert.That(InjectedProbeFixture.Created.Count).IsEqualTo(1);
        await Assert.That(InjectedProbeFixture.Created.Count(x => x.DisposeCount == 1)).IsEqualTo(1);
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
