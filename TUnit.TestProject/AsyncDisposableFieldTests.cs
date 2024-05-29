using TUnit.Core;

namespace TUnit.TestProject;

public class AsyncDisposableFieldTests
{
    private TextWriter? _textWriter;

    [BeforeEachTest]
    public void Setup()
    {
        _textWriter = new StringWriter();
    }

    [AfterEachTest]
    public async Task Blah()
    {
        await _textWriter!.DisposeAsync();
    }

    [Test]
    public void Test1()
    {
    }
}