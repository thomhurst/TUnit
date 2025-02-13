namespace TUnit.TestProject;

public class AsyncDisposableTests : IAsyncDisposable
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

    public async ValueTask DisposeAsync()
    {
        await Task.CompletedTask;
    }
}