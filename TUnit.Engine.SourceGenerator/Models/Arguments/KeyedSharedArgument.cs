namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record KeyedSharedArgument : Argument
{
    public KeyedSharedArgument(string type, string? invocation) : base(type, invocation)
    {
    }
    
    public required string Key { get; init; }
}