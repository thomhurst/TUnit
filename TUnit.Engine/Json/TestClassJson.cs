namespace TUnit.Engine.Json;

public record TestClassJson
{
    public required string? ClassName { get; init; }
    public required TestJson[] Tests { get; init; }
}