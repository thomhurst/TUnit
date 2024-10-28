namespace TUnit.Core.SourceGenerator.Models.Arguments;

public record Argument
{
    public Argument(string type, string? invocation)
    {
        Type = type;
        Invocation = invocation ?? "null";
    }

    public string Type { get; }
    public string Invocation { get; } 
}