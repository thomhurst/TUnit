using System.Text.Json.Serialization;

namespace TUnit.Core;

public record TestResult
{
    /// <summary>
    /// Only final states should be used.
    /// </summary>
    public required TestState State { get; init; }

    public required DateTimeOffset? Start { get; init; }

    public required DateTimeOffset? End { get; init; }

    public required TimeSpan? Duration { get; init; }

    public required Exception? Exception { get; init; }

    public required string ComputerName { get; init; }

    public string? Output { get; internal set; }

    [JsonIgnore]
    internal TestContext? TestContext { get; init; }

    /// <summary>
    /// The reason provided when this result was overridden.
    /// </summary>
    public string? OverrideReason { get; init; }

    /// <summary>
    /// Indicates whether this result was explicitly overridden via <see cref="TestContext.Execution.OverrideResult"/>.
    /// </summary>
    public bool IsOverridden { get; init; }

    /// <summary>
    /// The original exception that occurred before the result was overridden.
    /// Useful for debugging and audit trails when a test failure is overridden to pass or skip.
    /// </summary>
    public Exception? OriginalException { get; init; }
}
