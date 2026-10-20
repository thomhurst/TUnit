using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7157;

// The engine fetches each class data row once before it evaluates the method data sources, then
// once per test. With SharedType.None the first fetch created an extra instance that no test held,
// so it was never disposed. Every instance a non-shared class data source creates must belong to a
// test, and be disposed with it, in both engine modes (#7157).

public sealed class ClassRowDisposable : IDisposable
{
    public static ConcurrentBag<ClassRowDisposable> Created { get; } = [];

    public ClassRowDisposable() => Created.Add(this);

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

public sealed class MethodRowsClassRowDisposable : IDisposable
{
    public static ConcurrentBag<MethodRowsClassRowDisposable> Created { get; } = [];

    public MethodRowsClassRowDisposable() => Created.Add(this);

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<ClassRowDisposable>(Shared = SharedType.None)]
public class NonSharedClassDataTests(ClassRowDisposable data)
{
    [Test]
    public async Task One() => await Assert.That(data.IsDisposed).IsFalse();

    [Test]
    public async Task Two() => await Assert.That(data.IsDisposed).IsFalse();
}

[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<MethodRowsClassRowDisposable>(Shared = SharedType.None)]
public class NonSharedClassDataWithMethodRowsTests(MethodRowsClassRowDisposable data)
{
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Repeat(1)]
    public async Task Run(int value) => await Assert.That(data.IsDisposed).IsFalse();
}

public sealed class InstanceSourceFixture : IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

// An instance method data source needs a temporary instance of the class, built from the class row's
// first fetch and disposed once the method rows are read. That instance owns the first fetch, so no
// test may take it: a class that disposes its injected fixture would otherwise close the first test's.
[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<InstanceSourceFixture>(Shared = SharedType.None)]
public class InstanceMethodDataSourceDisposingFixtureTests(InstanceSourceFixture fixture) : IDisposable
{
    public IEnumerable<int> Rows() => [1, 2];

    [Test]
    [InstanceMethodDataSource(nameof(Rows))]
    public async Task Run(int value) => await Assert.That(fixture.IsDisposed).IsFalse();

    public void Dispose() => fixture.Dispose();
}

[EngineTest(ExpectedResult.Pass)]
public class FirstClassDataFetchObserverTests
{
    [Test]
    [DependsOn(typeof(NonSharedClassDataTests))]
    [DependsOn(typeof(NonSharedClassDataWithMethodRowsTests))]
    public async Task Every_Non_Shared_Class_Data_Instance_Belongs_To_A_Test_And_Is_Disposed()
    {
        // One instance per test: two tests, then two method rows run twice each.
        await Assert.That(ClassRowDisposable.Created.Count).IsEqualTo(2);
        await Assert.That(ClassRowDisposable.Created.All(x => x.IsDisposed)).IsTrue();

        await Assert.That(MethodRowsClassRowDisposable.Created.Count).IsEqualTo(4);
        await Assert.That(MethodRowsClassRowDisposable.Created.All(x => x.IsDisposed)).IsTrue();
    }
}
