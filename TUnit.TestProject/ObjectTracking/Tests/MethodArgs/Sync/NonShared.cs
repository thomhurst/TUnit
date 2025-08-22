namespace TUnit.TestProject.ObjectTracking.Tests.MethodArgs.Sync;

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

    [Test]
    [DependsOn(nameof(AssertExpectedDisposed))]
    [ClassDataSource<Disposable>(Shared = SharedType.PerClass)]
    public async Task Test3(Disposable disposable)
    {
        var test1 = TestContext.Current!.GetTests(nameof(Test1))[0];
        var previousDisposable = (Disposable) test1.TestDetails.TestMethodArguments[0]!;

        await Assert.That(disposable).IsNotSameReferenceAs(previousDisposable);
    }

    [DependsOn(nameof(Test3))]
    [Test]
    public async Task AssertExpectedDisposed2()
    {
        var test3 = TestContext.Current!.GetTests(nameof(Test3))[0];
        var disposable = (Disposable) test3.TestDetails.TestMethodArguments[0]!;

        await Assert.That(disposable.IsDisposed).IsTrue();
    }
}
