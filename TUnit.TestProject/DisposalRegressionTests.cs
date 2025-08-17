using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

/// <summary>
/// Regression test for disposal issue #2918
/// Ensures test class instances and their injected properties are properly disposed
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class DisposalRegressionTests : IAsyncDisposable
{
    private static bool _testInstanceDisposed = false;
    private static bool _injectedPropertyDisposed = false;

    [ClassDataSource<DisposableTestData>(Shared = SharedType.PerTestSession)]
    public required DisposableTestData InjectedData { get; init; }

    public ValueTask DisposeAsync()
    {
        _testInstanceDisposed = true;
        return default;
    }

    [Test]
    public async Task TestExecutes()
    {
        // Reset disposal flags at start of test
        _testInstanceDisposed = false;
        _injectedPropertyDisposed = false;

        // Test should execute normally
        await Assert.That(_testInstanceDisposed).IsFalse();
        await Assert.That(_injectedPropertyDisposed).IsFalse();
    }

    [After(Class)]
    public static async Task VerifyDisposal()
    {
        // After the test class is done, both the test instance and injected property should be disposed
        await Assert.That(_testInstanceDisposed).IsTrue();
        await Assert.That(_injectedPropertyDisposed).IsTrue();
    }

    public class DisposableTestData : IAsyncDisposable
    {
        public string Value { get; set; } = "test-data";

        public ValueTask DisposeAsync()
        {
            _injectedPropertyDisposed = true;
            return default;
        }
    }
}
