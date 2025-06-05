using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._2449;

[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<SampleDataClass>]
public sealed class Tests(SampleDataClass arg)
{
    [Test]
    public async Task Test1()
    {
        var value = arg.Value;

        await Assert.That(value).IsTrue();
    }

    [Test]
    [DependsOn(nameof(Test1))]
    public async Task Test2()
    {
        var value = arg.Value;

        await Assert.That(value).IsTrue();
    }
}

public sealed class SampleDataClass : IAsyncInitializer, IAsyncDisposable
{
    public bool Value { get; } = true;

    public Task InitializeAsync() => Task.CompletedTask;
    public ValueTask DisposeAsync() => default;
}