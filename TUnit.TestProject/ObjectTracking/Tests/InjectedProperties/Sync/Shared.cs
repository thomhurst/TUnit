namespace TUnit.TestProject.ObjectTracking.Tests.InjectedProperties.Sync;

public class Shared
{
    [ClassDataSource<Disposable>(Shared = SharedType.PerClass)]
    public required Disposable Disposable { get; init; }

    [Test]
    public void Test1()
    {
    }

    [DependsOn(nameof(Test1))]
    [Test]
    public async Task AssertExpectedDisposed()
    {
        var test1 = (Shared)TestContext.Current!.GetTests(nameof(Test1))[0].TestDetails.ClassInstance;
        var disposable = test1.Disposable;

        await Assert.That(disposable.IsDisposed).IsFalse();
    }

    [Test]
    [DependsOn(nameof(AssertExpectedDisposed))]
    public async Task Test3()
    {
        var test1 = (Shared)TestContext.Current!.GetTests(nameof(Test1))[0].TestDetails.ClassInstance;
        var disposable = test1.Disposable;

        await Assert.That(disposable).IsSameReferenceAs(Disposable);
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
