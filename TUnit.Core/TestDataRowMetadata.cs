namespace TUnit.Core;

/// <summary>
/// Metadata extracted from a <see cref="TestDataRow{T}"/> wrapper or data source attributes.
/// </summary>
/// <param name="DisplayName">Custom display name for the test case.</param>
/// <param name="Skip">Skip reason - test will be skipped if set.</param>
/// <param name="Categories">Categories to apply to the test case.</param>
internal record TestDataRowMetadata(
    string? DisplayName,
    string? Skip,
    string[]? Categories
)
{
    /// <summary>
    /// Returns true if any metadata property is set.
    /// </summary>
    public bool HasMetadata => DisplayName is not null || Skip is not null || Categories is { Length: > 0 };

    /// <summary>
    /// Merges this metadata with another, preferring non-null values from this instance.
    /// </summary>
    public TestDataRowMetadata MergeWith(TestDataRowMetadata? other)
    {
        if (other is null)
        {
            return this;
        }

        return new TestDataRowMetadata(
            DisplayName ?? other.DisplayName,
            Skip ?? other.Skip,
            Categories ?? other.Categories
        );
    }
}
