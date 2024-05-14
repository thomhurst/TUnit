using TUnit.Core;

namespace TUnit.TestProject;

public class BasicTests
{
    [Test]
    public void SynchronousTest()
    {
    }
    
    [Test]
    public async Task AsynchronousTest()
    {
        await Task.CompletedTask;
    }
}