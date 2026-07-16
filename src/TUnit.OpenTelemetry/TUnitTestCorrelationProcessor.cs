using System.Diagnostics;
using OpenTelemetry;
using TUnit.Core;

namespace TUnit.OpenTelemetry;

/// <summary>
/// Copies the <c>tunit.test.id</c> baggage item from the ambient Activity onto
/// every span as a tag, so spans produced by libraries with broken parent
/// chains can still be filtered by test in backends like Jaeger or Seq.
/// </summary>
/// <remarks>
/// Tagging runs at both <see cref="OnStart"/> and <see cref="OnEnd"/>:
/// <list type="bullet">
///   <item>Spans with an in-process parent (the common case) pick up the baggage at start via
///   <see cref="Activity.GetBaggageItem"/>'s parent-chain walk — OnStart is enough.</item>
///   <item>Spans with a remote-context parent (e.g. ASP.NET Core server spans created from an
///   extracted <c>traceparent</c>) receive baggage via the propagator <em>after</em>
///   <see cref="ActivitySource.StartActivity(string, ActivityKind)"/> returns, so only OnEnd
///   can see it.</item>
/// </list>
/// Fallback lookup uses <see cref="TraceRegistry"/> keyed on the activity's own
/// <see cref="Activity.TraceId"/>, not <see cref="Activity.Current"/> — this avoids
/// cross-attribution when a span outlives its test's async context and is stopped on a
/// thread where <see cref="Activity.Current"/> belongs to a different concurrent test.
/// <para>
/// Tag writes during <see cref="OnEnd"/> become visible to downstream processors and
/// exporters that defer serialization — the default <c>BatchExportProcessor</c>, and
/// reference-capturing exporters like <c>InMemoryExporter</c>. Synchronous pipelines
/// (e.g. <c>SimpleExportProcessor</c>) that serialize inside their own <c>OnEnd</c>
/// only observe the tag if this processor is registered before them.
/// </para>
/// <para>
/// Pass a <see cref="CorrelationScope"/> when the processor is registered against a
/// per-factory <c>TracerProvider</c>: the processor will then refuse to stamp activities
/// whose request pipeline carries a different factory's id, preventing the cross-factory
/// leak that broke <c>OptOut_DoesNotTag_AspNetCoreSpans</c>.
/// </para>
/// </remarks>
public sealed class TUnitTestCorrelationProcessor : BaseProcessor<Activity>
{
    private readonly CorrelationScope? _scope;

    public TUnitTestCorrelationProcessor()
        : this(scope: null)
    {
    }

    public TUnitTestCorrelationProcessor(CorrelationScope? scope)
    {
        _scope = scope;
    }

    public override void OnStart(Activity activity)
    {
        // When scoped to a factory, the per-request baggage that identifies the owning
        // factory is set by middleware running AFTER ActivitySource.StartActivity, so
        // OnStart can't yet tell whether this activity belongs to us. ASP.NET Core
        // server activities have no in-process parent in test context anyway —
        // OnEnd-based tagging covers the meaningful path.
        if (_scope is not null)
        {
            return;
        }

        TryTag(activity);
    }

    public override void OnEnd(Activity activity)
    {
        TryTag(activity);
    }

    private void TryTag(Activity activity)
    {
        if (activity.GetTagItem(TUnitActivitySource.TagTestId) is not null)
        {
            return;
        }

        if (_scope is not null)
        {
            // A scoped processor only tags activities whose request pipeline tagged
            // them with this factory's id. Without this guard, factory A's processor
            // would happily stamp activities triggered by factory B's request — both
            // factories observe the same Activity object via the process-global
            // Microsoft.AspNetCore source, and any tag it writes is visible to every
            // factory's exporter.
            var factoryId = activity.GetBaggageItem(CorrelationScope.FactoryIdBaggageKey);
            if (factoryId != _scope.FactoryId)
            {
                return;
            }
        }

        var testId = activity.GetBaggageItem(TUnitActivitySource.TagTestId)
            ?? TraceRegistry.GetContextId(activity.TraceId.ToString());

        if (testId is not null)
        {
            activity.SetTag(TUnitActivitySource.TagTestId, testId);
        }
    }
}
