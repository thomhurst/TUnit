namespace TUnit.Engine.Json;

internal record TestSessionJson
{
    public required TestAssemblyJson[] Assemblies { get; init; }
}
