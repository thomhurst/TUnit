using System.Collections.Concurrent;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7213;

// Tracking a test's arguments reads their IAsyncInitializer-typed properties. When such a getter
// throws, the argument objects themselves already exist, so they must still be disposed, whether the
// test fails during registration or is skipped (#7213).

public abstract class ThrowingGetterRoot : IDisposable
{
    public bool IsDisposed { get; private set; }

    public IAsyncInitializer Child => throw new InvalidOperationException("getter throws");

    public void Dispose() => IsDisposed = true;
}

public sealed class FailedTestThrowingGetterRoot : ThrowingGetterRoot
{
    public static ConcurrentBag<FailedTestThrowingGetterRoot> Created { get; } = [];

    public FailedTestThrowingGetterRoot() => Created.Add(this);
}

public sealed class SkippedTestThrowingGetterRoot : ThrowingGetterRoot
{
    public static ConcurrentBag<SkippedTestThrowingGetterRoot> Created { get; } = [];

    public SkippedTestThrowingGetterRoot() => Created.Add(this);
}

[EngineTest(ExpectedResult.Failure)]
public class ThrowingGetterFailedTests
{
    [Test]
    [ClassDataSource<FailedTestThrowingGetterRoot>(Shared = SharedType.None)]
    public void FailsDuringRegistration(FailedTestThrowingGetterRoot value)
    {
    }
}

[EngineTest(ExpectedResult.Pass)]
public class ThrowingGetterSkippedTests
{
    [Test, Skip("skipped")]
    [ClassDataSource<SkippedTestThrowingGetterRoot>(Shared = SharedType.None)]
    public void SkippedWithThrowingGetter(SkippedTestThrowingGetterRoot value) => throw new Exception("test ran");
}

// Marked Failure (not Pass) despite passing itself: DependsOn pulls FailsDuringRegistration into any
// filtered run containing this test, which would fail the EngineTest=Pass run.
[EngineTest(ExpectedResult.Failure)]
public class ThrowingGetterRootDisposalObserverTests
{
    [Test]
    [DependsOn(typeof(ThrowingGetterFailedTests), ProceedOnFailure = true)]
    [DependsOn(typeof(ThrowingGetterSkippedTests), ProceedOnFailure = true)]
    public async Task Roots_Are_Disposed_When_Tracking_Throws()
    {
        await Assert.That(FailedTestThrowingGetterRoot.Created.Count).IsEqualTo(1);
        await Assert.That(FailedTestThrowingGetterRoot.Created.All(x => x.IsDisposed)).IsTrue();

        await Assert.That(SkippedTestThrowingGetterRoot.Created.Count).IsEqualTo(1);
        await Assert.That(SkippedTestThrowingGetterRoot.Created.All(x => x.IsDisposed)).IsTrue();
    }
}
