using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._3961;

public sealed class BugResource : IAsyncInitializer, IAsyncDisposable
{
    public static int TearDowns = 0;
    public static int SetUps = 0;
    public  ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref TearDowns);
        return default;
    }

    public Task InitializeAsync()
    {
        Interlocked.Increment(ref SetUps);
        return Task.CompletedTask;
    }
}

[EngineTest(ExpectedResult.Pass)]
public sealed class BugTestTwo
{
    [ClassDataSource<BugResource>]
    public required BugResource BugResource { get; init; }

    [Test]
    public async Task TestTwo()
    {
        await Assert.That(BugResource.SetUps).EqualTo(1);
    }
}

[EngineTest(ExpectedResult.Pass)]
public sealed class BugTestThree
{
    [ClassDataSource<BugResource>]
    public required BugResource BugResource { get; init; }

    [Test]
    public async Task TestThree()
    {
        await Assert.That(BugResource.SetUps).EqualTo(1);
    }
}

[EngineTest(ExpectedResult.Pass)]
public sealed class BugTestOne
{
    [ClassDataSource<BugResource>]
    public required BugResource BugResource { get; init; }

    [Test]
    public async Task TestOne()
    {
        await Assert.That(BugResource.SetUps).EqualTo(1);
    }
}
