namespace TUnit.TestProject.Bugs._5982;

using TUnit.TestProject.Attributes;

public sealed class MyFixture : IAsyncDisposable
{
    public bool Disposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}

[EngineTest(ExpectedResult.Pass)]
public class AnotherTest
{
    [ClassDataSource<MyFixture>(Shared = SharedType.PerTestSession)]
    public required MyFixture My { get; set; }

    [Test]
    [Arguments(100)]
    [Arguments(100)]
    [Arguments(100)]
    [Arguments(100)]
    [Arguments(100)]
    [Arguments(100)]
    public async Task Wait(int delay)
    {
        await Task.Delay(delay);
    }
}

[EngineTest(ExpectedResult.Pass)]
[NotInParallel]
public class SomeTest
{
    [ClassDataSource<MyFixture>(Shared = SharedType.PerTestSession)]
    public required MyFixture My { get; set; }

    [Test]
    [MethodDataSource(nameof(Methods))]
    public async Task TheLastCaseFails(int delay)
    {
        await Assert.That(My.Disposed).IsFalse();
        await Task.Delay(delay);
    }

    public IEnumerable<Func<int>> Methods()
    {
        yield return () => 100;
        yield return () => 100;
        yield return () => 100;
        yield return () => 100;
    }

    [Test]
    [Arguments(100)]
    [Arguments(100)]
    [Arguments(100)]
    [Arguments(100)]
    [Arguments(100)]
    [Arguments(100)]
    public async Task Wait1(int delay)
    {
        await Task.Delay(delay);
    }

    [Test]
    public async Task Wait2()
    {
        await Task.Delay(100);
    }

    [Test]
    public async Task Wait3()
    {
        await Task.Delay(500);
    }

    [Test]
    public async Task Wait4()
    {
        await Task.Delay(2_000);
    }
}
