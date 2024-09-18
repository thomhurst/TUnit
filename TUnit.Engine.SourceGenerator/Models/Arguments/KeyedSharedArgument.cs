namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record KeyedSharedArgument : Argument
{
    public KeyedSharedArgument(string type, string? invocation, bool isUnfoldableTuple = false) : base(type, invocation, isUnfoldableTuple)
    {
    }
    
    public required string Key { get; init; }
}