using System.Diagnostics;
using OpenTelemetry;

namespace TUnit.OpenTelemetry;

/// <summary>
/// Copies the <c>tunit.test.id</c> baggage item from the ambient Activity onto
/// every new span as a tag, so spans produced by libraries with broken parent
/// chains can still be filtered by test in backends like Jaeger or Seq.
/// </summary>
public sealed class TUnitTestCorrelationProcessor : BaseProcessor<Activity>
{
    private const string TestIdTag = "tunit.test.id";

    public override void OnStart(Activity activity)
    {
        if (activity.GetTagItem(TestIdTag) is not null)
        {
            return;
        }

        var testId = Activity.Current?.GetBaggageItem(TestIdTag);
        if (testId is not null)
        {
            activity.SetTag(TestIdTag, testId);
        }
    }
}
