namespace TUnit.TestProject.ObjectTracking.Tests.InjectedProperties.Async;

public class Shared
{
    [ClassDataSource<AsyncDisposable>(Shared = SharedType.PerClass)]
    public required AsyncDisposable Disposable { get; init; }

    [Test]
    public void Test1()
    {
    }

    [DependsOn(nameof(Test1))]
    [Test]
    public async Task AssertExpectedDisposed()
    {
        // Access the shared object from our own instance, not from Test1's instance
        await Assert.That(Disposable.IsDisposed).IsFalse();
    }

    [Test]
    [DependsOn(nameof(AssertExpectedDisposed))]
    public async Task Test3()
    {
        // Get the shared object from Test1's instance to verify it's the same reference
        var test1 = (Shared)TestContext.Current!.GetTests(nameof(Test1))[0].TestDetails.ClassInstance;
        var disposable1 = test1.Disposable;

        await Assert.That(Disposable).IsSameReferenceAs(disposable1);
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
