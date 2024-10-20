namespace TUnit.TestProject;

public class DisposablePropertyTests
{
#pragma warning disable TUnit0023
    public HttpClient? HttpClient { get; private set; }
#pragma warning restore TUnit0023

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