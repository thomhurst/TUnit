namespace TUnit.TestProject;

[Category("Smoke")]
public class BasicTests
{
    [Test]
    public void SynchronousTest()
    {
        // Dummy method
    }

    [Test]
    public async Task AsynchronousTest()
    {
        await Task.CompletedTask;
    }

    [Test]
    public async ValueTask ValueTaskAsynchronousTest()
    {
        await new ValueTask();
    }
}
