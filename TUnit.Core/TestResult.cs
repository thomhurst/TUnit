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

    public string? OverrideReason { get; set; }
    public bool IsOverridden { get; set; }
}
