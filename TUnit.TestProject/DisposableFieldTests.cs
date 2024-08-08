namespace TUnit.TestProject;

public class DisposableFieldTests
{
    private HttpClient? _httpClient;

    [Before(EachTest)]
    public void Setup()
    {
        _httpClient = new HttpClient();
    }

    [After(EachTest)]
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