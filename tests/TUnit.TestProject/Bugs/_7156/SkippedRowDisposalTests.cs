using System.Collections.Concurrent;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7156;

// A skipped test never registers its arguments, but the builder has already created them.
// Those objects must still be disposed, in both engine modes, and a shared instance that another
// test still uses must not be disposed early (#7156).

public abstract class SkippedRowDisposable : IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

public sealed class SkippedMethodDisposable : SkippedRowDisposable
{
    public static ConcurrentBag<SkippedMethodDisposable> Created { get; } = [];

    public SkippedMethodDisposable() => Created.Add(this);
}

public sealed class SkippedDataRowDisposable : SkippedRowDisposable
{
    public static ConcurrentBag<SkippedDataRowDisposable> Created { get; } = [];

    public SkippedDataRowDisposable() => Created.Add(this);
}

public sealed class SkippedSharedDisposable : SkippedRowDisposable
{
    public static ConcurrentBag<SkippedSharedDisposable> Created { get; } = [];

    public SkippedSharedDisposable() => Created.Add(this);
}

// Tracking a skipped test's arguments reads its IAsyncInitializer-typed properties. A getter that
// throws must leave the test skipped, not abort the run.
public sealed class SkippedThrowingGetterHolder
{
    public IAsyncInitializer Inner => throw new InvalidOperationException("getter throws");
}

// The getter runs while the skipped test's arguments are tracked, so it must see that test as the
// current one, as getters do while a running test's arguments are registered.
public sealed class SkippedContextRecordingHolder
{
    public static ConcurrentBag<string?> SeenTestNames { get; } = [];

    public IAsyncInitializer? Inner
    {
        get
        {
            SeenTestNames.Add(TestContext.Current?.Metadata.TestName);
            return null;
        }
    }
}

[EngineTest(ExpectedResult.Pass)]
public class SkippedRowTests
{
    [Test, Skip("skipped")]
    [ClassDataSource<SkippedContextRecordingHolder>(Shared = SharedType.None)]
    public void SkippedWithContextReadingGetter(SkippedContextRecordingHolder value) => throw new Exception("test ran");

    [Test, Skip("skipped")]
    [ClassDataSource<SkippedThrowingGetterHolder>(Shared = SharedType.None)]
    public void SkippedWithThrowingGetter(SkippedThrowingGetterHolder value) => throw new Exception("test ran");

    [Test, Skip("skipped")]
    [ClassDataSource<SkippedMethodDisposable>(Shared = SharedType.None)]
    public void SkippedWithClassDataSource(SkippedMethodDisposable value) => throw new Exception("test ran");

    public static IEnumerable<TestDataRow<SkippedDataRowDisposable>> Rows()
    {
        yield return new TestDataRow<SkippedDataRowDisposable>(new SkippedDataRowDisposable(), Skip: "row skipped");
    }

    [Test]
    [MethodDataSource(nameof(Rows))]
    public void SkippedDataRow(SkippedDataRowDisposable value) => throw new Exception("test ran");

    // The keyed instance is also injected into SkippedRowDisposalObserverTests, which still uses it
    // after this test is skipped, so the skipped test must only release its reference.
    [Test, Skip("skipped")]
    [ClassDataSource<SkippedSharedDisposable>(Shared = SharedType.Keyed, Key = "7156")]
    public void SkippedWithSharedClassDataSource(SkippedSharedDisposable value) => throw new Exception("test ran");
}

[EngineTest(ExpectedResult.Pass)]
public class SkippedRowDisposalObserverTests
{
    [ClassDataSource<SkippedSharedDisposable>(Shared = SharedType.Keyed, Key = "7156")]
    public required SkippedSharedDisposable Shared { get; init; }

    [Test]
    [DependsOn(typeof(SkippedRowTests), ProceedOnFailure = true)]
    public async Task Objects_Of_Skipped_Rows_Are_Disposed_But_Shared_Ones_Stay_Alive()
    {
        await Assert.That(SkippedMethodDisposable.Created.Count).IsEqualTo(1);
        await Assert.That(SkippedMethodDisposable.Created.All(x => x.IsDisposed)).IsTrue();

        await Assert.That(SkippedDataRowDisposable.Created.Count).IsEqualTo(1);
        await Assert.That(SkippedDataRowDisposable.Created.All(x => x.IsDisposed)).IsTrue();

        await Assert.That(SkippedContextRecordingHolder.SeenTestNames).IsNotEmpty();
        await Assert.That(SkippedContextRecordingHolder.SeenTestNames.All(x => x == nameof(SkippedRowTests.SkippedWithContextReadingGetter))).IsTrue();

        await Assert.That(SkippedSharedDisposable.Created.Count).IsEqualTo(1);
        await Assert.That(Shared.IsDisposed).IsFalse();
    }
}
