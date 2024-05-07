using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models;

internal record Argument
{
    public Argument(ArgumentSource argumentSource, string type, string invocation)
    {
        ArgumentSource = argumentSource;
        Type = type;
        Invocation = MapValue(type, invocation);
    }

    public ArgumentSource ArgumentSource { get; }
    public string Type { get; }
    public string Invocation { get; }

    private static string MapValue(string type, string value)
    {
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