using System.Net;
using Microsoft.Extensions.DependencyInjection;
using TUnit.AspNetCore;
using TUnit.AspNetCore.Interception;

namespace TUnit.AspNetCore.Tests;

/// <summary>
/// <see cref="WebApplicationTest{TFactory,TEntryPoint}.HttpCapture"/> must expose the capture store that the
/// capture middleware writes to, which is the singleton registered in the per-test factory's container.
/// </summary>
public class HttpCaptureTests : WebApplicationTest<TestWebAppFactory, Program>
{
    protected override void ConfigureTestOptions(WebApplicationTestOptions options)
    {
        options.EnableHttpExchangeCapture = true;
    }

    [Test]
    public async Task HttpCapture_Records_Requests_Made_Through_Factory()
    {
        using var client = Factory.CreateClient();
        var response = await client.GetAsync("/ping");
        response.EnsureSuccessStatusCode();

        await Assert.That(HttpCapture).IsNotNull();
        await Assert.That(HttpCapture!.Count).IsEqualTo(1);
        await Assert.That(HttpCapture.Last!.Request.Path).IsEqualTo("/ping");
        await Assert.That(HttpCapture.Last.Response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task HttpCapture_Is_The_Registered_Capture_Service()
    {
        await Assert.That(HttpCapture).IsSameReferenceAs(Services.GetRequiredService<HttpExchangeCapture>());
    }
}

/// <summary>
/// The documented way to configure capture options: enable capture and register a configured
/// <see cref="HttpExchangeCapture"/> in <c>ConfigureTestServices</c>. The user's registration must win,
/// and requests must be recorded once.
/// </summary>
public class HttpCaptureCustomRegistrationTests : WebApplicationTest<TestWebAppFactory, Program>
{
    protected override void ConfigureTestOptions(WebApplicationTestOptions options)
    {
        options.EnableHttpExchangeCapture = true;
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddHttpExchangeCapture(capture => capture.MaxBodySize = 4);
    }

    [Test]
    public async Task HttpCapture_Uses_User_Registration_And_Records_Once()
    {
        using var client = Factory.CreateClient();
        var response = await client.GetAsync("/ping");
        response.EnsureSuccessStatusCode();

        await Assert.That(HttpCapture!.MaxBodySize).IsEqualTo(4);
        await Assert.That(HttpCapture.Count).IsEqualTo(1);
    }
}

public class HttpCaptureDisabledTests : WebApplicationTest<TestWebAppFactory, Program>
{
    [Test]
    public async Task HttpCapture_Is_Null_When_Not_Enabled()
    {
        await Assert.That(HttpCapture).IsNull();
    }
}
