namespace TUnit.Engine.Json;

internal record TestAssemblyJson
{
    public required string? AssemblyName { get; init; }
    public required TestClassJson[] Classes { get; init; }
}
