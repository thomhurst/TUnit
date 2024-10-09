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
        Console.WriteLine("Yeehaw");
        await Task.CompletedTask;
    }
}