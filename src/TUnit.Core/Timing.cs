namespace TUnit.Core;

[Obsolete("Use OpenTelemetry activity spans instead. Hook timings are now automatically recorded as OTel child spans of the test activity.")]
public record Timing(string StepName, DateTimeOffset Start, DateTimeOffset End)
{
    public TimeSpan Duration => End - Start;
}
