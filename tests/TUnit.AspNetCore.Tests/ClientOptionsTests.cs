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

    [Test]
    public async Task CreateClient_Redirect_KeepsPropagationHeadersOnRedirectedRequest()
    {
        using var client = Factory.CreateClient();

        await AssertRedirectedRequestHasTestIdHeader(client);
    }

    internal static async Task AssertRedirectedRequestHasTestIdHeader(HttpClient client)
    {
        var response = await client.GetAsync("/redirect-to-echo-headers");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var lines = (await response.Content.ReadAsStringAsync()).Split('\n');

        // Exact line match proves a single value: /echo-headers joins repeated values with
        // commas, so a header duplicated across redirect hops would not match this line.
        await Assert.That(lines).Contains(TUnitTestIdHandler.HeaderName + ": " + TestContext.Current!.Id);
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
    public async Task CreateClient_FollowsRedirects_ByDefault()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("/redirect");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await response.Content.ReadAsStringAsync()).IsEqualTo("pong");
    }

    [Test]
    public async Task CreateClient_Redirect_KeepsPropagationHeadersOnRedirectedRequest()
    {
        using var client = Factory.CreateClient();

        await ClientOptionsTests.AssertRedirectedRequestHasTestIdHeader(client);
    }

    [Test]
    public async Task CreateClient_WithOptions_CanDisableCookiesAndRedirects()
    {
        using var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            AllowAutoRedirect = false,
        });

        await ClientOptionsTests.AssertCookieRoundTrip(client, expected: "<none>");

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
}

/// <summary>
/// A <c>ConfigureClient</c> override must still apply to clients created through the
/// option-aware <c>CreateClient</c> overloads.
/// </summary>
public class ConfigureClientOverrideTests
{
    [ClassDataSource(Shared = [SharedType.PerTestSession])]
    public ConfigureClientWebAppFactory Factory { get; set; } = null!;

    [Test]
    public async Task CreateClient_RunsConfigureClientOverride()
    {
        using var client = Factory.CreateClient();

        var echoed = await client.GetStringAsync("/echo-headers");

        await Assert.That(echoed).Contains(ConfigureClientWebAppFactory.HeaderName + ": yes");
    }
}

public class ConfigureClientWebAppFactory : TestWebAppFactory
{
    public const string HeaderName = "X-Configured-By-Override";

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Add(HeaderName, "yes");
    }
}
