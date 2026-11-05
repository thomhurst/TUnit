using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using TUnit.TestProject.Attributes;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._7205;

// DependencyInjectionDataSourceAttribute<TScope> registers cleanup on the builder context while
// fetching its row. Originally that cleanup never reached the executable test. Every scope must be
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

    public InjectedFixtureAlias Alias => new(this);
}

public sealed class InjectedFixtureAlias(NestedProbeService service) : IAsyncInitializer
{
    public InjectedProbeFixture Fixture => service.Fixture;

    public Task InitializeAsync() => Task.CompletedTask;
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
    public NestedProbeIntermediate Child { get; } = new();
    public bool IsInitialized { get; private set; }
    public int DisposeCount => _disposeCount;
    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }
    public void Dispose()
    {
        Interlocked.Increment(ref _disposeCount);
        Child.Leaf.Dispose();
    }
}

// Ownership must survive a non-disposable initializer between two disposable dependencies.
public sealed class NestedProbeIntermediate : IAsyncInitializer
{
    public NestedProbeLeaf Leaf { get; } = new();
    public Task InitializeAsync() => Task.CompletedTask;
}

public sealed class NestedProbeLeaf : IAsyncInitializer, IDisposable
{
    public bool IsInitialized { get; private set; }
    public int DisposeCount { get; private set; }
    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }
    public void Dispose() => DisposeCount++;
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

// A test skipped because its dependency failed takes the coordinator early-skip path. The scope created for
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
        await Assert.That(service.Dependency.Child.Leaf.IsInitialized).IsTrue();
        await Assert.That(service.Dependency.Child.Leaf.DisposeCount).IsEqualTo(0);
        await Assert.That(service.Dependency.DisposeCount).IsEqualTo(0);
        await Assert.That(service.Fixture.IsInitialized).IsTrue();
        await Assert.That(service.Fixture.DisposeCount).IsEqualTo(0);
    }
}

public sealed class FailedClassRowDependencyInjectionAttribute() : ProbeDependencyInjectionAttribute(nameof(FailedClassRowScopeTests));

public sealed class FailedMethodRowDependencyInjectionAttribute() : ProbeDependencyInjectionAttribute(nameof(FailedMethodRowScopeTests));

[EngineTest(ExpectedResult.Failure)]
[FailedClassRowDependencyInjection]
// The optional argument also lets the generator emit the placeholder for a constraint-invalid row.
public class FailedClassRowScopeTests(ProbeService? service = null)
{
    [Test]
    [Arguments("not a struct")]
    public Task Run<T>(T value) where T : struct => Task.CompletedTask;
}

[EngineTest(ExpectedResult.Failure)]
public class FailedMethodRowScopeTests
{
    [Test]
    [FailedMethodRowDependencyInjection]
    public Task Run<T>(T value) where T : struct => Task.CompletedTask;
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
    [DependsOn(typeof(NonDisposableScopeTests))]
    [DependsOn(typeof(NestedScopeDependencyTests))]
    [DependsOn(typeof(FailedClassRowScopeTests), ProceedOnFailure = true)]
    [DependsOn(typeof(FailedMethodRowScopeTests), ProceedOnFailure = true)]
    public async Task Every_Scope_Is_Disposed_When_Its_Test_Ends()
    {
        // One scope per test: two tests, then two method rows run twice each.
        await AssertScopes(nameof(ClassRowScopeTests), 2);
        await AssertScopes(nameof(ClassRowScopeWithMethodRowsTests), 4);

        // One scope for the temporary instance that reads the method rows, then one per test. Only the
        // tests' scopes are disposed when they end; the temporary instance's scope lives until session cleanup.
        await AssertScopes(nameof(ClassRowScopeWithInstanceMethodDataSourceTests), 3, expectedDisposed: 2);

        // A method parameter scope per test run.
        await AssertScopes(nameof(MethodParameterScopeTests), 2);

        // One scope for both attempts, and one for each skipped test.
        await AssertScopes(nameof(RetriedScopeTests), 1);
        await AssertScopes(nameof(FailedClassRowScopeTests), 1);
        await AssertScopes(nameof(FailedMethodRowScopeTests), 1);
        await AssertScopes(nameof(SkippedScopeTests), 1);
        await AssertScopes(nameof(SkippedIfEmptyScopeTests), 1);

        // The scope of the test skipped because its dependency failed.
        await AssertScopes(nameof(DependencySkippedScopeTests), 1);

        // One scope for each test removed by circular-dependency detection. Depending on those tests would
        // put this test in their cycle, but the scheduler removes them and disposes their scopes before any
        // test runs.
        await AssertScopes(nameof(CircularDependencyScopeTests), 2);

        await Assert.That(UnownedService.Created.Count).IsEqualTo(1);
        await Assert.That(UnownedService.Created.Count(x => x.DisposeCount == 1)).IsEqualTo(1);
        var dependencies = NestedProbeScope.Created.SelectMany(x => x.Dependencies).ToArray();
        await Assert.That(dependencies.Length).IsEqualTo(1);
        await Assert.That(dependencies.Count(x => x.DisposeCount == 1)).IsEqualTo(1);
        await Assert.That(dependencies.Count(x => x.Child.Leaf.DisposeCount == 1)).IsEqualTo(1);
        await Assert.That(InjectedProbeFixture.Created.Count).IsEqualTo(1);
        await Assert.That(InjectedProbeFixture.Created.Count(x => x.DisposeCount == 1)).IsEqualTo(1);
    }

    private static async Task AssertScopes(string owner, int expectedCount, int? expectedDisposed = null)
    {
        var scopes = ProbeScope.Created.Where(x => x.Owner == owner).ToArray();

        await Assert.That(scopes.Length).IsEqualTo(expectedCount);

        // Disposed once each: a test raises its OnDispose once, not once per attempt or per raising path.
        await Assert.That(scopes.Count(x => x.DisposeCount == 1)).IsEqualTo(expectedDisposed ?? expectedCount);
        await Assert.That(scopes.Count(x => x.DisposeCount > 1)).IsEqualTo(0);
        var services = scopes.SelectMany(x => x.Services).ToArray();
        await Assert.That(services.Count(x => x.DisposeCount == 1)).IsEqualTo(expectedDisposed ?? expectedCount);
        await Assert.That(services.Count(x => x.DisposeCount > 1)).IsEqualTo(0);
    }
}
