#if NET

namespace TUnit.TestProject;

public class AsyncDisposableFieldTests
{
    private TextWriter? _textWriter;

    [Before(Test)]
    public void Setup()
    {
        _textWriter = new StringWriter();
    }

    [After(Test)]
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
#endif
