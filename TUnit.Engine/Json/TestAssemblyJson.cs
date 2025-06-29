namespace TUnit.Engine.Json;

public record TestAssemblyJson
{
    public required string? AssemblyName { get; init; }
    public required TestClassJson[] Classes { get; init; }
}
