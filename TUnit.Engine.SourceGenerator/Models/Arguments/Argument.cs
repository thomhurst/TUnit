namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record Argument
{
    public Argument(string type, string? invocation)
    {
        Type = type;
        Invocation = invocation ?? "null";
    }

    public string Type { get; }
    public string Invocation { get; }
    public string[]? TupleVariableNames { get; init; }
    public bool DisposeAfterTest { get; init; }
}