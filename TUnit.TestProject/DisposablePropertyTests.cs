namespace TUnit.TestProject;

public class DisposablePropertyTests
{
    public HttpClient? HttpClient { get; private set; }

    [Before(EachTest)]
    public void Setup()
    {
        HttpClient = new HttpClient();
    }

    [After(EachTest)]
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