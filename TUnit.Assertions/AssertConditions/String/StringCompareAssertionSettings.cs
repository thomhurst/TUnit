namespace TUnit.Assertions.AssertConditions.String;

public record StringCompareAssertionSettings
{
    public StringComparison StringComparison { get; init; } = StringComparison.Ordinal;
    public bool Trim { get; init; } = false;
};