namespace TUnit.Engine.Json;

internal record TestClassJson
{
    public required string? Type { get; init; }
    public required TestJson[] Tests { get; init; }
}
