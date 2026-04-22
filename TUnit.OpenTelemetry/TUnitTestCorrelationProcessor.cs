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
/// </remarks>
public sealed class TUnitTestCorrelationProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        TryTag(activity);
    }

    public override void OnEnd(Activity activity)
    {
        TryTag(activity);
    }

    private static void TryTag(Activity activity)
    {
        if (activity.GetTagItem(TUnitActivitySource.TagTestId) is not null)
        {
            return;
        }

        var testId = activity.GetBaggageItem(TUnitActivitySource.TagTestId);
        if (testId is null && !ReferenceEquals(Activity.Current, activity))
        {
            testId = Activity.Current?.GetBaggageItem(TUnitActivitySource.TagTestId);
        }

        if (testId is not null)
        {
            activity.SetTag(TUnitActivitySource.TagTestId, testId);
        }
    }
}
