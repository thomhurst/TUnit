namespace TUnit.TestProject;

public class AsyncDisposableFieldTests
{
    private TextWriter? _textWriter;

    [Before(EachTest)]
    public void Setup()
    {
        _textWriter = new StringWriter();
    }

    [After(EachTest)]
    public async Task Blah()
    {
        await _textWriter!.DisposeAsync();
    }

    [Test]
    public void Test1()
    {
        // Dummy method
    }
}