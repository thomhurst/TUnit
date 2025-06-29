using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class DisposableTests : IDisposable
{
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
        foreach (var disposableTestse in context.Tests.Select(x => x.TestDetails.ClassInstance).OfType<DisposableTests>())
        {
            await Assert.That(disposableTestse.IsDisposed).IsTrue();
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
