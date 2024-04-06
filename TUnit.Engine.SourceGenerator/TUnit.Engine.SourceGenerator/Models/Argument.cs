namespace TUnit.Engine.SourceGenerator.Models;

internal record Argument(string Type, string Invocation)
{
    public static readonly Argument NoArguments = new("NONE", "NONE");
}