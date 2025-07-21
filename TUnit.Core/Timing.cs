namespace TUnit.Core;

public record Timing(string StepName, DateTimeOffset Start, DateTimeOffset End)
{
    public TimeSpan Duration => End - Start;
}
