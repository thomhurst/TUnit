using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs.Bug3219;

[EngineTest(ExpectedResult.Pass)]
public class Issue3219DataClass : IAsyncInitializer, IAsyncDisposable
{
    public int Value { get; private set; } = 0;

    public Task InitializeAsync()
    {
        Value = 42;
        Console.WriteLine($"Issue3219DataClass initialized, Value = {Value}");
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        Console.WriteLine($"Issue3219DataClass disposing, Value was {Value}");
        Value = -1;
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(10)); // Simulate some async disposal work
    }
}

public class Issue3219ReproTest
{
    [ClassDataSource<Issue3219DataClass>(Shared = SharedType.PerTestSession)]
    public required Issue3219DataClass DataClass { get; init; }

    [Test]
    [Retry(3)]
    public async Task Basic()
    {
        Console.WriteLine($"Test running, DataClass.Value = {DataClass.Value}");

        // This should be 42 on all retry attempts, not -1 after the first failure
        await Assert.That(DataClass.Value).IsEqualTo(42);

        // Always throw to force retries
        throw new Exception("This test always fails");
    }
}
