namespace TUnit.TestProject;

public class AsyncDisposablePropertyTests
{
    public TextWriter? TextWriter { get; private set; }

    [Before(Test)]
    public void Setup()
    {
        TextWriter = new StringWriter();
    }

    [After(Test)]
    public async Task Blah()
    {
        await TextWriter!.DisposeAsync();
    }

    [Test]
    public void Test1()
    {
        // Dummy method
    }
}