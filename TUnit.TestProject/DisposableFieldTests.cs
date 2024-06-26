using TUnit.Core;

namespace TUnit.TestProject;

public class DisposableFieldTests
{
    private HttpClient? _httpClient;

    [BeforeEachTest]
    public void Setup()
    {
        _httpClient = new HttpClient();
    }

    [AfterEachTest]
    public void Blah()
    {
        _httpClient?.Dispose();
    }

    [Test]
    public void Test1()
    {
        // Dummy method
    }
}