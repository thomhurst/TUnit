namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record Argument
{
    public Argument(string type, string? invocation, bool isTuple = false)
    {
        Type = type;
        IsTuple = isTuple;
        Invocation = MapValue(type, invocation, isTuple);
    }

    public string Type { get; }
    public bool IsTuple { get; }
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