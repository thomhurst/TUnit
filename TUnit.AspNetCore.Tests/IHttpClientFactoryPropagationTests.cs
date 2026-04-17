using System.Diagnostics;
using TUnit.AspNetCore;
using TUnit.Core;

namespace TUnit.AspNetCore.Tests;

/// <summary>
/// Coverage for thomhurst/TUnit#5590 — the SUT's <see cref="IHttpClientFactory"/> pipelines
/// must automatically carry the test's trace context when
/// <see cref="WebApplicationTestOptions.AutoPropagateHttpClientFactory"/> is <c>true</c>.
/// </summary>
public class IHttpClientFactoryPropagationTests : WebApplicationTest<TestWebAppFactory, Program>
{
    [Test]
    public async Task SutHttpClientFactory_Propagates_TestContextHeaders()
    {
        using var activity = new Activity("outer-test-span").Start();

        using var client = Factory.CreateClient();

        var response = await client.GetAsync("/proxy");
        response.EnsureSuccessStatusCode();

        var echoed = await response.Content.ReadAsStringAsync();

        await Assert.That(echoed).Contains("traceparent:");
        await Assert.That(echoed).Contains(TUnitTestIdHandler.HeaderName + ": " + TestContext.Current!.Id);
        await Assert.That(echoed).Contains("baggage:");
        await Assert.That(echoed).Contains(activity.TraceId.ToString());
    }
}

public class IHttpClientFactoryPropagationOptOutTests : WebApplicationTest<TestWebAppFactory, Program>
{
    protected override void ConfigureTestOptions(WebApplicationTestOptions options)
    {
        options.AutoPropagateHttpClientFactory = false;
    }

    [Test]
    public async Task SutHttpClientFactory_DoesNotPropagate_WhenAutoPropagationDisabled()
    {
        using var activity = new Activity("outer-test-span").Start();

        using var client = Factory.CreateClient();

        var response = await client.GetAsync("/proxy");
        response.EnsureSuccessStatusCode();

        var echoed = await response.Content.ReadAsStringAsync();

        await Assert.That(echoed).DoesNotContain(TUnitTestIdHandler.HeaderName);
        await Assert.That(echoed).DoesNotContain("traceparent:");
        await Assert.That(echoed).DoesNotContain("baggage:");
    }
}
