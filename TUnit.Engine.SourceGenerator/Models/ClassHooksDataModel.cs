namespace TUnit.Engine.SourceGenerator.Models;

public record ClassHooksDataModel
{
    public required string FullyQualifiedTypeName { get; init; }
    public required string MinimalTypeName { get; init; }
    public required string MethodName { get; init; }
    public required bool HasParameters { get; init; }
}