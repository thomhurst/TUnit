using System.Text.Json.Serialization;
using TUnit.Core.Enums;

namespace TUnit.Core;

/// <summary>
/// Represents the result of a test.
/// </summary>
public record TestResult
{
    /// <summary>
    /// Gets or sets the status of the test.
    /// </summary>
    public required Status Status { get; init; }

    /// <summary>
    /// Gets or sets the start time of the test.
    /// </summary>
    public required DateTimeOffset? Start { get; init; }

    /// <summary>
    /// Gets or sets the end time of the test.
    /// </summary>
    public required DateTimeOffset? End { get; init; }

    /// <summary>
    /// Gets or sets the duration of the test.
    /// </summary>
    public required TimeSpan? Duration { get; init; }

    /// <summary>
    /// Gets or sets the exception thrown during the test, if any.
    /// </summary>
    public required Exception? Exception { get; init; }

    /// <summary>
    /// Gets or sets the name of the computer where the test was run.
    /// </summary>
    public required string ComputerName { get; init; }

    /// <summary>
    /// Gets or sets the output of the test.
    /// </summary>
    public string? Output { get; internal set; }

    /// <summary>
    /// Gets or sets the test context.
    /// </summary>
    [JsonIgnore]
    internal TestContext? TestContext { get; init; }
    
    public string? OverrideReason { get; set; }
    public bool IsOverridden { get; set; }
}