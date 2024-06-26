using TUnit.Core;

namespace TUnit.TestProject;

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
}