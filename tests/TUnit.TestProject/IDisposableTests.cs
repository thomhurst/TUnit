using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class DisposableTests : IDisposable
{
    // Finished tests release their class instance (#7260), so keep our own references to inspect.
    private static readonly ConcurrentBag<DisposableTests> Instances = [];

    public DisposableTests()
    {
        Instances.Add(this);
    }

    [Test]
    public void One()
    {

    }

    [Test]
    public Task Two()
    {
        return Task.CompletedTask;
    }

    [Test]
    public async Task Three()
    {
        await Task.CompletedTask;
    }

    [After(Class)]
    public static async Task AssertDisposed(ClassHookContext context)
    {
        await Assert.That(Instances).IsNotEmpty();

        foreach (var disposableTests in Instances)
        {
            await Assert.That(disposableTests.IsDisposed).IsTrue();
        }
    }

    public void Dispose()
    {
        IsDisposed = true;
    }

    public bool IsDisposed
    {
        get;
        private set;
    }
}
