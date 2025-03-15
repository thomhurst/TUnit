namespace TUnit.Core;

/// <summary>
/// Represents the timing information for a test step.
/// </summary>
/// <param name="StepName">The name of the step.</param>
/// <param name="Start">The start time of the step.</param>
/// <param name="End">The end time of the step.</param>
public record Timing(string StepName, DateTimeOffset Start, DateTimeOffset End)
{
    /// <summary>
    /// Gets the duration of the step.
    /// </summary>
    public TimeSpan Duration => End - Start;
}