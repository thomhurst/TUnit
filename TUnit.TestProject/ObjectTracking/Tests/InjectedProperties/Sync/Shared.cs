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

    [After(Class)]
    public static async Task AssertExpectedDisposed2(ClassHookContext classHookContext)
    {
        var tests = classHookContext.Tests;
        var test1 = (Shared)tests.FirstOrDefault(x => x.TestName == nameof(Test1))!.TestDetails.ClassInstance;
        var test3 = (Shared)tests.FirstOrDefault(x => x.TestName == nameof(Test3))!.TestDetails.ClassInstance;

        var disposable1 = test1.Disposable;
        var disposable3 = test3.Disposable;

        await Assert.That(disposable3).IsSameReferenceAs(disposable1);
        await Assert.That(disposable3.IsDisposed).IsTrue();
    }
}
