using System.Net.Http;

namespace TUnit.TestProject;

public class DisposableFieldTests
{
    private HttpClient? _httpClient;

    [Before(Test)]
    public void Setup()
    {
        _httpClient = new HttpClient();
    }

    [After(Test)]
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