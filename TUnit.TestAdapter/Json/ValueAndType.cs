namespace TUnit.TestAdapter.Json;

public record ValueAndType
{
    public required string? QualifiedTypeName { get; init; }
    public required object? Value { get; init; }
}