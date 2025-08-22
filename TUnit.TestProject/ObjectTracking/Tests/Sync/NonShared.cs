namespace TUnit.TestProject.ObjectTracking.Tests.Sync;

public class NonShared
{
    [Test]
    [ClassDataSource<Disposable>]
    public void Test1(Disposable disposable)
    {
    }

    [DependsOn(nameof(Test1))]
    [Test]
    public async Task AssertExpectedDisposed()
    {
        var test1 = TestContext.Current!.GetTests(nameof(Test1))[0];
        var disposable = (Disposable) test1.TestDetails.TestMethodArguments[0]!;

        await Assert.That(disposable.IsDisposed).IsTrue();
    }
}
