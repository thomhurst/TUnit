using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using TUnit.AspNetCore;
using TUnit.Core;

namespace TUnit.AspNetCore.Tests;

/// <summary>
/// Regression coverage for thomhurst/TUnit#6921: clients created by
/// <see cref="TestWebApplicationFactory{TEntryPoint}"/> must honor
/// <see cref="WebApplicationFactoryClientOptions"/> (cookies, redirects, base address)
/// while still carrying TUnit's propagation headers.
/// </summary>
public class ClientOptionsTests
{
    [ClassDataSource(Shared = [SharedType.PerTestSession])]
    public TestWebAppFactory Factory { get; set; } = null!;

    [Test]
    public async Task CreateClient_HandlesCookies_ByDefault()
    {
        using var client = Factory.CreateClient();

        await AssertCookieRoundTrip(client, expected: "abc");
    }

    [Test]
    public async Task CreateClient_FollowsRedirects_ByDefault()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("/redirect");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await response.Content.ReadAsStringAsync()).IsEqualTo("pong");
    }

    [Test]
    public async Task CreateClient_WithOptions_CanDisableCookiesAndRedirects()
    {
        using var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            AllowAutoRedirect = false,
        });

        await AssertCookieRoundTrip(client, expected: "<none>");

        var response = await client.GetAsync("/redirect");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
    }

    [Test]
    public async Task CreateClient_WithOptions_UsesBaseAddress()
    {
        var baseAddress = new Uri("http://tunit.test/");

        using var client = Factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = baseAddress });

        await Assert.That(client.BaseAddress).IsEqualTo(baseAddress);
    }

    [Test]
    public async Task CreateClient_WithOptions_KeepsPropagationHeaders()
    {
        using var client = Factory.CreateClient(new WebApplicationFactoryClientOptions());

        var echoed = await client.GetStringAsync("/echo-headers");

        await Assert.That(echoed).Contains(TUnitTestIdHandler.HeaderName + ": " + TestContext.Current!.Id);
    }

    internal static async Task AssertCookieRoundTrip(HttpClient client, string expected)
    {
        var set = await client.GetAsync("/cookie/set/abc");
        set.EnsureSuccessStatusCode();

        var value = await client.GetStringAsync("/cookie/get");

        await Assert.That(value).IsEqualTo(expected);
    }
}

public class WebApplicationTestClientOptionsTests : WebApplicationTest<TestWebAppFactory, Program>
{
    [Test]
    public async Task CreateClient_HandlesCookies_ByDefault()
    {
        using var client = Factory.CreateClient();

        await ClientOptionsTests.AssertCookieRoundTrip(client, expected: "abc");
    }

    [Test]
    public async Task CreateClient_WithOptions_CanDisableCookies()
    {
        using var client = Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        await ClientOptionsTests.AssertCookieRoundTrip(client, expected: "<none>");
    }

    [Test]
    public async Task CreateClient_WithOptions_KeepsPropagationHeaders()
    {
        using var client = Factory.CreateClient(new WebApplicationFactoryClientOptions());

        var echoed = await client.GetStringAsync("/echo-headers");

        await Assert.That(echoed).Contains(TUnitTestIdHandler.HeaderName + ": " + TestContext.Current!.Id);
    }
}
