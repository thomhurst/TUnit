using TUnit.Core;

namespace TUnit.TestProject;

public class AsyncDisposablePropertyTests
{
    public TextWriter? TextWriter { get; private set; }

    [BeforeEachTest]
    public void Setup()
    {
        TextWriter = new StringWriter();
    }

    [AfterEachTest]
    public async Task Blah()
    {
        await TextWriter!.DisposeAsync();
    }

    [Test]
    public void Test1()
    {
    }
}