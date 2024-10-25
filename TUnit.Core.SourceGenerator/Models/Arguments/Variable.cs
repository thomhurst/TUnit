namespace TUnit.Core.SourceGenerator.Models.Arguments;

public record Variable
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required string Value { get; init; }

    public override string ToString()
    {
        return $"{Type} {Name} = {Value};";
    }
}