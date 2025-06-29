namespace TUnit.Engine.Json;

public record TestSessionJson
{
    public required TestAssemblyJson[] Assemblies { get; init; }
}
