using TUnit.Core;

namespace TUnit.TestProject;

public class DisposablePropertyTests
{
    public HttpClient? HttpClient { get; private set; }

    [BeforeEachTest]
    public void Setup()
    {
        HttpClient = new HttpClient();
    }

    [AfterEachTest]
    public void Blah()
    {
        HttpClient?.Dispose();
    }

    [Test]
    public void Test1()
    {
        // Dummy method
    }
}