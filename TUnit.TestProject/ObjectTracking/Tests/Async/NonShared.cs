namespace TUnit.TestProject.ObjectTracking.Tests.Async;

public class NonShared
{
    [Test]
    [ClassDataSource<AsyncDisposable>]
    public void Test1(AsyncDisposable disposable)
    {
    }

    [DependsOn(nameof(Test1))]
    [Test]
    public async Task AssertExpectedDisposed()
    {
        var test1 = TestContext.Current!.GetTests(nameof(Test1))[0];
        var disposable = (AsyncDisposable) test1.TestDetails.TestMethodArguments[0]!;

        await Assert.That(disposable.IsDisposed).IsTrue();
    }
}
