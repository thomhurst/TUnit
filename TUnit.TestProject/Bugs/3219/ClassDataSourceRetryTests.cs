using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs.Bug3219;

public class DataClass : IAsyncInitializer, IAsyncDisposable
{
    public int Value { get; private set; }
    private static int _initCount;
    private static int _disposeCount;

    public static int InitCount => _initCount;
    public static int DisposeCount => _disposeCount;

    public static void ResetCounters()
    {
        _initCount = 0;
        _disposeCount = 0;
    }

    public Task InitializeAsync()
    {
        _initCount++;
        Value = 42;
        Console.WriteLine($"DataClass initialized (count: {_initCount}), Value set to {Value}");
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _disposeCount++;
        Console.WriteLine($"DataClass disposing (count: {_disposeCount}), Value was {Value}");
        Value = -1;
        return default;
    }
}

[EngineTest(ExpectedResult.Pass)]
public class ClassDataSourceRetryTests
{
    private static int _attemptCount;

    [ClassDataSource<DataClass>(Shared = SharedType.PerTestSession)]
    public required DataClass DataClass { get; init; }

    [BeforeEvery(Test)]
    public static void ResetAttemptCount()
    {
        if (_attemptCount == 0)
        {
            // Reset counters only on the first test run
            DataClass.ResetCounters();
        }
    }

    [Test]
    [Retry(2)]
    public async Task TestThatFailsAndRetries()
    {
        _attemptCount++;
        Console.WriteLine($"Test attempt {_attemptCount}, DataClass.Value = {DataClass.Value}, InitCount = {DataClass.InitCount}, DisposeCount = {DataClass.DisposeCount}");

        // The value should be 42 on all attempts (not -1 after disposal)
        await Assert.That(DataClass.Value).IsEqualTo(42);

        // Fail on attempts 1 and 2, succeed on attempt 3
        if (_attemptCount < 3)
        {
            throw new Exception($"Deliberate failure on attempt {_attemptCount}");
        }

        // On the successful attempt (3rd), verify that initialization only happened once
        // and disposal hasn't happened yet (will happen after test finalization)
        Console.WriteLine($"Success on attempt {_attemptCount}! Checking counts...");
        await Assert.That(DataClass.DisposeCount).IsEqualTo(0);
    }

    [Test]
    [DependsOn(nameof(TestThatFailsAndRetries))]
    public async Task VerifyDisposalAfterRetries()
    {
        // After the previous test completed (including all retries),
        // the DataClass should now be disposed
        await Assert.That(DataClass.DisposeCount).IsEqualTo(1);
    }
}
