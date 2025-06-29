namespace TUnit.Engine.Json;

public record TestClassJson
{
    public required string? Type { get; init; }
    public required TestJson[] Tests { get; init; }
}
