using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class UriAssertionTests
{
    [Test]
    public async Task Test_Uri_IsAbsoluteUri()
    {
        var uri = new Uri("https://example.com/path");
        await Assert.That(uri).IsAbsoluteUri();
    }

    [Test]
    public async Task Test_Uri_IsAbsoluteUri_Http()
    {
        var uri = new Uri("http://localhost:8080");
        await Assert.That(uri).IsAbsoluteUri();
    }

    [Test]
    public async Task Test_Uri_IsNotAbsoluteUri()
    {
        var uri = new Uri("/relative/path", UriKind.Relative);
        await Assert.That(uri).IsNotAbsoluteUri();
    }

    [Test]
    public async Task Test_Uri_IsFile()
    {
        var uri = new Uri("file:///C:/temp/file.txt");
        await Assert.That(uri).IsFile();
    }

    [Test]
    public async Task Test_Uri_IsFile_LocalPath()
    {
        var uri = new Uri(@"C:\temp\file.txt");
        await Assert.That(uri).IsFile();
    }

    [Test]
    public async Task Test_Uri_IsNotFile()
    {
        var uri = new Uri("https://example.com");
        await Assert.That(uri).IsNotFile();
    }

    [Test]
    public async Task Test_Uri_IsUnc()
    {
        var uri = new Uri(@"\\server\share\file.txt");
        await Assert.That(uri).IsUnc();
    }

    [Test]
    public async Task Test_Uri_IsNotUnc()
    {
        var uri = new Uri("https://example.com");
        await Assert.That(uri).IsNotUnc();
    }

    [Test]
    public async Task Test_Uri_IsLoopback()
    {
        var uri = new Uri("http://localhost/path");
        await Assert.That(uri).IsLoopback();
    }

    [Test]
    public async Task Test_Uri_IsLoopback_127001()
    {
        var uri = new Uri("http://127.0.0.1/path");
        await Assert.That(uri).IsLoopback();
    }

    [Test]
    public async Task Test_Uri_IsNotLoopback()
    {
        var uri = new Uri("https://example.com");
        await Assert.That(uri).IsNotLoopback();
    }

    [Test]
    public async Task Test_Uri_IsDefaultPort_Http()
    {
        var uri = new Uri("http://example.com");
        await Assert.That(uri).IsDefaultPort();
    }

    [Test]
    public async Task Test_Uri_IsDefaultPort_Https()
    {
        var uri = new Uri("https://example.com");
        await Assert.That(uri).IsDefaultPort();
    }

    [Test]
    public async Task Test_Uri_IsNotDefaultPort()
    {
        var uri = new Uri("http://example.com:8080");
        await Assert.That(uri).IsNotDefaultPort();
    }

    [Test]
    public async Task Test_Uri_UserEscaped()
    {
        var uri = new Uri("http://example.com/path%20with%20spaces", dontEscape: true);
        await Assert.That(uri).UserEscaped();
    }

    [Test]
    public async Task Test_Uri_IsNotUserEscaped()
    {
        var uri = new Uri("http://example.com/path with spaces");
        await Assert.That(uri).IsNotUserEscaped();
    }
}
