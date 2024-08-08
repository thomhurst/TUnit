namespace TUnit.TestProject;

public class AsyncDisposablePropertyTests
{
    public TextWriter? TextWriter { get; private set; }

    [Before(EachTest)]
    public void Setup()
    {
        TextWriter = new StringWriter();
    }

    [After(EachTest)]
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