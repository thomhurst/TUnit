using System.Diagnostics;
using OpenTelemetry;
using TUnit.Core;

namespace TUnit.OpenTelemetry;

/// <summary>
/// Copies the <c>tunit.test.id</c> baggage item from the ambient Activity onto
/// every new span as a tag, so spans produced by libraries with broken parent
/// chains can still be filtered by test in backends like Jaeger or Seq.
/// </summary>
public sealed class TUnitTestCorrelationProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        if (activity.GetTagItem(TUnitActivitySource.TagTestId) is not null)
        {
            return;
        }

        var testId = Activity.Current?.GetBaggageItem(TUnitActivitySource.TagTestId);
        if (testId is not null)
        {
            activity.SetTag(TUnitActivitySource.TagTestId, testId);
        }
    }
}
