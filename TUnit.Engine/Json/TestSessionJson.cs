namespace TUnit.Engine.Json;

internal record TestSessionJson
{
    public required TestJson[] Tests { get; init; }
}