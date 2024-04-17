namespace TUnit.Engine.SourceGenerator.Models;

internal record Argument
{
    public static readonly Argument NoArguments = new("NONE", "NONE");

    public Argument(string type, string invocation)
    {
        Type = type;
        Invocation = MapValue(type, invocation);
    }
    
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