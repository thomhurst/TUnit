using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7123;

// A data-source row whose inferred type argument breaks a generic constraint ends as a failed test.
// The objects its factory created must still be disposed, in both engine modes, and a shared
// instance that another test still uses must not be disposed early (#7123).

public sealed class PerRowDisposable : IDisposable
{
    public static ConcurrentBag<PerRowDisposable> Created { get; } = [];

    public PerRowDisposable() => Created.Add(this);

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

public sealed class PerRowDisposableDataSource : DataSourceGeneratorAttribute<PerRowDisposable>
{
    protected override IEnumerable<Func<PerRowDisposable>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => new PerRowDisposable();
        yield return () => new PerRowDisposable();
    }
}

public sealed class SharedRowDisposable : IDisposable
{
    public static ConcurrentBag<SharedRowDisposable> Created { get; } = [];

    public SharedRowDisposable() => Created.Add(this);

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

[EngineTest(ExpectedResult.Failure)]
public class ConstraintViolatingRowTests
{
    // PerRowDisposable is a class, so both rows break `where T : struct` and fail.
    [Test]
    [PerRowDisposableDataSource]
    public async Task Run<T>(T value) where T : struct => await Task.CompletedTask;
}

[EngineTest(ExpectedResult.Failure)]
public class SharedConstraintViolatingRowTests
{
    // The keyed instance is also injected into FailedRowDisposalObserverTests, which still uses it
    // after this row fails, so the failed row must only release its reference.
    [Test]
    [ClassDataSource<SharedRowDisposable>(Shared = SharedType.Keyed, Key = "7123")]
    public async Task Run<T>(T value) where T : struct => await Task.CompletedTask;
}

// Marked Failure (not Pass) despite passing itself: DependsOn pulls the failing classes into any
// filtered run containing this test. Verified by Issue7123Tests in TUnit.Engine.Tests.
[EngineTest(ExpectedResult.Failure)]
public class FailedRowDisposalObserverTests
{
    [ClassDataSource<SharedRowDisposable>(Shared = SharedType.Keyed, Key = "7123")]
    public required SharedRowDisposable Shared { get; init; }

    [Test]
    [DependsOn(typeof(ConstraintViolatingRowTests), ProceedOnFailure = true)]
    [DependsOn(typeof(SharedConstraintViolatingRowTests), ProceedOnFailure = true)]
    public async Task Objects_Of_Failed_Rows_Are_Disposed_But_Shared_Ones_Stay_Alive()
    {
        await Assert.That(PerRowDisposable.Created.Count).IsEqualTo(2);
        await Assert.That(PerRowDisposable.Created.All(x => x.IsDisposed)).IsTrue();

        await Assert.That(SharedRowDisposable.Created.Count).IsEqualTo(1);
        await Assert.That(Shared.IsDisposed).IsFalse();
    }
}
