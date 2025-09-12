using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

/// <summary>
/// Regression test for disposal issue #2918
/// Ensures test class instances and their injected properties are properly disposed
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class DisposalRegressionTests : IAsyncDisposable
{
    [ClassDataSource<DisposableTestData>(Shared = SharedType.PerTestSession)]
    public required DisposableTestData InjectedData { get; init; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return default;
    }

    public bool IsDisposed { get; private set; }

    [Test]
    public async Task TestExecutes()
    {
        // Test should execute normally
        await Assert.That(IsDisposed).IsFalse();
        await Assert.That(InjectedData.IsDisposed).IsFalse();
    }

    [After(Class)]
    public static async Task VerifyDisposal(ClassHookContext context)
    {
        // After the test class is done, both the test instance and injected property should be disposed
        foreach (var testInstance in context.Tests.Select(x => x.TestDetails.ClassInstance).OfType<DisposalRegressionTests>())
        {
            await Assert.That(testInstance.IsDisposed).IsTrue();
            await Assert.That(testInstance.InjectedData.IsDisposed).IsTrue();
        }
    }

    public class DisposableTestData : IAsyncDisposable
    {
        public string Value { get; set; } = "test-data";

        public bool IsDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return default;
        }
    }
}
