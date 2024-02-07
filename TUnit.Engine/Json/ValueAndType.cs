namespace TUnit.Engine.Json;

internal record ValueAndType
{
    public required string? QualifiedTypeName { get; init; }
    public required object? Value { get; init; }
}