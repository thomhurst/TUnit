namespace TUnit.Assertions;

public record ComparisonFailure
{
    public required MemberType Type { get; init; }
    public required string[] NestedMemberNames { get; init; }
    public required object? Actual { get; init; }
    public required object? Expected { get; init; }
}