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
        Console.WriteLine("Foo Bar!");
        await Task.CompletedTask;
    }

    public class Inner
    {
        [Test]
        public void SynchronousTest()
        {
            // Dummy method
        }
    }
}