using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record KeyedSharedArgument : Argument
{
    public KeyedSharedArgument(ArgumentSource argumentSource, string type, string? invocation, bool isTuple = false) : base(argumentSource, type, invocation, isTuple)
    {
    }
    
    public required string Key { get; init; }
}