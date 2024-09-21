namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record Argument
{
    public Argument(string type, string? invocation, bool isUnfoldableTuple = false)
    {
        Type = type;
        IsUnfoldableTuple = isUnfoldableTuple;
        Invocation = MapValue(type, invocation, isUnfoldableTuple);
    }

    public string Type { get; }
    public bool IsUnfoldableTuple { get; }
    public string Invocation { get; }
    public string[]? TupleVariableNames { get; init; }
    public bool DisposeAfterTest { get; init; }

    private static string MapValue(string type, string? value, bool isTuple)
    {
        type = type.TrimEnd('?');
        
        if (value is null)
        {
            return "null";
        }

        if (isTuple)
        {
            return value;
        }
        
        if (type == "global::System.Char")
        {
            return $"'{value}'";
        }
        
        if (type == "global::System.Boolean")
        {
            return value.ToLower();
        }
        
        if (type == "global::System.String")
        {
            return $"\"{value}\"";
        }

        return value;
    }
}