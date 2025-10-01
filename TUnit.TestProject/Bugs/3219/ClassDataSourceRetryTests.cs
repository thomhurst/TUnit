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
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _disposeCount++;
        Value = -1;
        IsDisposed = true;
        return default;
    }

    public bool IsDisposed { get; private set; }
}

[EngineTest(ExpectedResult.Pass)]
[NotInParallel]
public class ClassDataSourceRetryTests
{
    private static bool _wasExecuted;
    private static int _attemptCount;

    [ClassDataSource<DataClass>(Shared = SharedType.PerTestSession)]
    public required DataClass DataClass { get; init; }

    [Before(TestSession)]
    public static void ResetCountersForSession()
    {
        DataClass.ResetCounters();
        _attemptCount = 0;
    }

    [Test]
    [Retry(2)]
    public async Task TestThatFailsAndRetries()
    {
        _wasExecuted = true;
        _attemptCount++;

        await Assert.That(DataClass.Value).IsEqualTo(42);

        if (_attemptCount < 3)
        {
            throw new Exception($"Deliberate failure on attempt {_attemptCount}");
        }

        await Assert.That(DataClass.DisposeCount).IsEqualTo(0);
    }

    [Test]
    [DependsOn(nameof(TestThatFailsAndRetries))]
    public async Task VerifyNotDisposedDuringTests()
    {
        await Assert.That(DataClass.DisposeCount).IsEqualTo(0);
        await Assert.That(DataClass.Value).IsEqualTo(42);
    }

    [After(TestSession)]
    public static async Task VerifyDisposalAfterTestSession(TestSessionContext context)
    {
        var classInstance = context.TestClasses.FirstOrDefault(x => x.ClassType  == typeof(ClassDataSourceRetryTests))?.Tests.FirstOrDefault()?.TestDetails.ClassInstance as ClassDataSourceRetryTests;

        var dataClass = classInstance?.DataClass;

        if (dataClass != null)
        {
            await Assert.That(dataClass.IsDisposed).IsTrue();
        }
    }
}
