using System.Collections.Concurrent;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._3993;

/// <summary>
/// Regression test for issue #3993: IAsyncInitializer called 4 times instead of 3
/// when using ClassDataSource with MethodDataSource that accesses instance properties.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class IAsyncInitializerTests
{
    [ClassDataSource<PerTestCaseSource>(Shared = SharedType.None)]
    public required PerTestCaseSource PerTestCaseSource { get; init; }

    public IEnumerable<string> PerTestCaseStrings() => PerTestCaseSource.MyString;

    [Test]
    [MethodDataSource(nameof(PerTestCaseStrings))]
    public async Task ForNTestCases_ShouldInitNTimes(string s)
    {
        await Assert.That(s).IsNotNull();

        // Each of the 3 test cases should have its own isolated PerTestCaseSource instance
        // Since Shared = SharedType.None, each test gets a new instance
        // Therefore, InitializeAsync should be called exactly 3 times (once per test)
        // NOT 4 times (which would include the discovery instance)
        await Assert
            .That(PerTestCaseSource.SetUps)
            .IsEqualTo(3)
            .Because("each of the 3 test cases should be wrapped in its own isolated test class, " +
                     "causing a call to async init, but no more than those three calls should be needed. " +
                     "The discovery-time instance should NOT be initialized.");
    }

    [After(Class)]
    public static void ResetCounter()
    {
        // Reset for next run
        PerTestCaseSource.Reset();
    }
}

public class PerTestCaseSource : IAsyncInitializer
{
    public string[] MyString = [];
    private static int _setUps = 0;
    public static int SetUps => _setUps;

    public Task InitializeAsync()
    {
        Interlocked.Increment(ref _setUps);
        MyString = ["123", "321", "9210"];
        return Task.CompletedTask;
    }

    public static void Reset()
    {
        _setUps = 0;
    }
}
