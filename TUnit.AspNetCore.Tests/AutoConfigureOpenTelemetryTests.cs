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
/// AutoWires asserts presence of its own <c>TestId</c> tag, so foreign spans observed
/// via the global ASP.NET Core <see cref="System.Diagnostics.ActivitySource"/> do not
/// affect it — no key needed.
/// </remarks>
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

        // ASP.NET Core stops its server activity on a continuation that may outlive the
        // client response, so poll briefly instead of reading _exported synchronously.
        var deadline = Environment.TickCount64 + 5_000;
        Activity? taggedSpan = null;
        while (Environment.TickCount64 < deadline)
        {
            taggedSpan = _exported.FirstOrDefault(a => (a.GetTagItem(TUnitActivitySource.TagTestId) as string) == testId);
            if (taggedSpan is not null)
            {
                break;
            }
            await Task.Delay(20);
        }

        await Assert.That(taggedSpan).IsNotNull();
    }
}

/// <remarks>
/// Global <see cref="NotInParallelAttribute"/> (no key): asserts <em>absence</em> of any
/// <see cref="TUnitActivitySource.TagTestId"/> tag, but every <see cref="TracerProvider"/>
/// with <c>AddAspNetCoreInstrumentation()</c> subscribes to the process-global
/// <c>Microsoft.AspNetCore</c> <see cref="System.Diagnostics.ActivitySource"/> and a parallel
/// factory's correlation processor stamps spans with its own <c>TestId</c> before they reach
/// this exporter. A keyed constraint cannot enumerate every parallel <see cref="WebApplicationTest{TFactory,TEntryPoint}"/>;
/// draining alone is the only reliable isolation until per-factory filtering lands.
/// </remarks>
[NotInParallel]
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
