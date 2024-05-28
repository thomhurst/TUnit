using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models;

internal record Argument
{
    public Argument(ArgumentSource argumentSource, string type, string? invocation, bool isTuple = false)
    {
        ArgumentSource = argumentSource;
        Type = type;
        IsTuple = isTuple;
        Invocation = MapValue(type, invocation, isTuple);
    }

    public ArgumentSource ArgumentSource { get; }
    public string Type { get; }
    public bool IsTuple { get; }
    public string Invocation { get; }
    public string? TupleVariableNames { get; init; }

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