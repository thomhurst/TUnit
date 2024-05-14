namespace TUnit.Engine.SourceGenerator.Models;

public record TestLocation
{
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }
};