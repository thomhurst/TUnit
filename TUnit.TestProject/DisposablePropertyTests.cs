namespace TUnit.TestProject;

public class DisposablePropertyTests
{
    public HttpClient? HttpClient { get; private set; }

    [Before(Test)]
    public void Setup()
    {
        HttpClient = new HttpClient();
    }

    [After(Test)]
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