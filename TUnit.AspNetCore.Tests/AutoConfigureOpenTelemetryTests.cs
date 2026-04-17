using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using TUnit.AspNetCore;
using TUnit.Core;

namespace TUnit.AspNetCore.Tests;

/// <summary>
/// Coverage for thomhurst/TUnit#5594 — <see cref="TestWebApplicationFactory{TEntryPoint}"/>
/// automatically augments the SUT's <see cref="TracerProvider"/> with TUnit's
/// correlation processor + ASP.NET Core instrumentation.
/// </summary>
/// <remarks>
/// Serialized against sibling auto-wire tests because <see cref="OpenTelemetry.Sdk"/>
/// attaches a process-global <see cref="ActivityListener"/> per <c>TracerProvider</c>,
/// so a parallel factory's correlation processor can tag activities created by another
/// factory's SUT. Serializing keeps assertions observing only their own factory's wiring.
/// </remarks>
[NotInParallel(nameof(AutoConfigureOpenTelemetryTests))]
public class AutoConfigureOpenTelemetryTests : WebApplicationTest<TestWebAppFactory, Program>
{
    private readonly List<Activity> _exported = [];

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddOpenTelemetry().WithTracing(t => t.AddInMemoryExporter(_exported));
    }

    [Test]
    public async Task AutoWires_TagsAspNetCoreSpans_WithTestId()
    {
        using var client = Factory.CreateClient();
        var response = await client.GetAsync("/ping");
        response.EnsureSuccessStatusCode();

        var testId = TestContext.Current!.Id;
        var taggedSpan = _exported.FirstOrDefault(a => (a.GetTagItem(TUnitActivitySource.TagTestId) as string) == testId);
        await Assert.That(taggedSpan).IsNotNull();
    }
}

[NotInParallel(nameof(AutoConfigureOpenTelemetryTests))]
public class AutoConfigureOpenTelemetryOptOutTests : WebApplicationTest<TestWebAppFactory, Program>
{
    private readonly List<Activity> _exported = [];

    protected override void ConfigureTestOptions(WebApplicationTestOptions options)
    {
        options.AutoConfigureOpenTelemetry = false;
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddOpenTelemetry().WithTracing(t => t
            .AddAspNetCoreInstrumentation()
            .AddInMemoryExporter(_exported));
    }

    [Test]
    public async Task OptOut_DoesNotTag_AspNetCoreSpans()
    {
        using var client = Factory.CreateClient();
        var response = await client.GetAsync("/ping");
        response.EnsureSuccessStatusCode();

        foreach (var activity in _exported)
        {
            await Assert.That(activity.GetTagItem(TUnitActivitySource.TagTestId)).IsNull();
        }
    }
}
