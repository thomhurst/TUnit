namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record KeyedSharedArgument : Argument
{
    public KeyedSharedArgument(string type, string? invocation, bool isTuple = false) : base(type, invocation, isTuple)
    {
    }
    
    public required string Key { get; init; }
}