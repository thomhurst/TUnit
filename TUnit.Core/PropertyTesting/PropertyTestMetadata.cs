namespace TUnit.Core.PropertyTesting;

/// <summary>
/// Metadata for property-based tests to track generation and shrinking state
/// </summary>
public class PropertyTestMetadata
{
    /// <summary>
    /// The ID of the original test that generated this property test
    /// </summary>
    public Guid OriginalTestId { get; set; }

    /// <summary>
    /// The current shrink attempt number (0 for original test)
    /// </summary>
    public int ShrinkAttempt { get; set; }

    /// <summary>
    /// Maximum number of shrink attempts allowed before stopping
    /// </summary>
    public int MaxShrinkAttempts { get; set; } = 1000;

    /// <summary>
    /// The original failing inputs that triggered shrinking
    /// </summary>
    public object?[]? OriginalFailingInputs { get; set; }

    /// <summary>
    /// Random seed used for reproducible test generation
    /// </summary>
    public long RandomSeed { get; set; }

    /// <summary>
    /// Indicates whether this is a shrinking test (vs. original generated test)
    /// </summary>
    public bool IsShrinkingTest { get; set; }
}
