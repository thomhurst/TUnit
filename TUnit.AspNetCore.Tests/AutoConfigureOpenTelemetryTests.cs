using System.Collections;
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
/// affect it — no key needed. Parallel <see cref="WebApplicationTest{TFactory,TEntryPoint}"/>
/// instances stamp activities (subscribed via <c>AddAspNetCoreInstrumentation</c>) on
/// background continuations, so the exporter sink must be thread-safe — polling
/// <see cref="LockedActivityList"/> snapshots its items under a lock.
/// </remarks>
public class AutoConfigureOpenTelemetryTests : WebApplicationTest<TestWebAppFactory, Program>
{
    private readonly LockedActivityList _exported = [];

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
            foreach (var activity in _exported.Snapshot())
            {
                if (activity.GetTagItem(TUnitActivitySource.TagTestId) as string == testId)
                {
                    taggedSpan = activity;
                    break;
                }
            }
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
    private readonly LockedActivityList _exported = [];

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

        foreach (var activity in _exported.Snapshot())
        {
            await Assert.That(activity.GetTagItem(TUnitActivitySource.TagTestId)).IsNull();
        }
    }
}

/// <summary>
/// OpenTelemetry's <c>AddInMemoryExporter</c> calls <c>ICollection&lt;T&gt;.Add</c> on a
/// background batch thread without locking, so a plain <see cref="List{T}"/> races with
/// any reader (the test's poll loop) and intermittently throws "Collection was modified".
/// This wrapper synchronises every mutation and exposes a <see cref="Snapshot"/> for
/// safe iteration.
/// </summary>
internal sealed class LockedActivityList : ICollection<Activity>
{
    private readonly List<Activity> _items = [];

    public int Count
    {
        get { lock (_items) return _items.Count; }
    }

    public bool IsReadOnly => false;

    public void Add(Activity item) { lock (_items) _items.Add(item); }

    public void Clear() { lock (_items) _items.Clear(); }

    public bool Contains(Activity item) { lock (_items) return _items.Contains(item); }

    public void CopyTo(Activity[] array, int arrayIndex) { lock (_items) _items.CopyTo(array, arrayIndex); }

    public bool Remove(Activity item) { lock (_items) return _items.Remove(item); }

    public Activity[] Snapshot() { lock (_items) return _items.ToArray(); }

    public IEnumerator<Activity> GetEnumerator() => ((IEnumerable<Activity>)Snapshot()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
