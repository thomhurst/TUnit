namespace TUnit.TestProject.ObjectTracking.Tests.Sync;

public class Shared
{
    [Test]
    [ClassDataSource<Disposable>(Shared = SharedType.PerClass)]
    public void Test1(Disposable disposable)
    {
    }

    [DependsOn(nameof(Test1))]
    [Test]
    public async Task AssertExpectedDisposed()
    {
        var test1 = TestContext.Current!.GetTests(nameof(Test1))[0];
        var disposable = (Disposable) test1.TestDetails.TestMethodArguments[0]!;

        await Assert.That(disposable.IsDisposed).IsFalse();
    }

    [Test]
    [DependsOn(nameof(AssertExpectedDisposed))]
    [ClassDataSource<Disposable>(Shared = SharedType.PerClass)]
    public async Task Test3(Disposable disposable)
    {
        var test1 = TestContext.Current!.GetTests(nameof(Test1))[0];
        var previousDisposable = (AsyncDisposable) test1.TestDetails.TestMethodArguments[0]!;

        await Assert.That(disposable).IsSameReferenceAs(previousDisposable);
    }

    [DependsOn(nameof(Test3))]
    [Test]
    public async Task AssertExpectedDisposed2()
    {
        var test1 = TestContext.Current!.GetTests(nameof(Test3))[0];
        var disposable = (Disposable) test1.TestDetails.TestMethodArguments[0]!;

        await Assert.That(disposable.IsDisposed).IsTrue();
    }
}
