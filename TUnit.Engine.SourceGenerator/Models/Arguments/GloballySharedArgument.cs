using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record GloballySharedArgument : Argument
{
    public GloballySharedArgument(ArgumentSource argumentSource, string type, string? invocation, bool isTuple = false) : base(argumentSource, type, invocation, isTuple)
    {
    }
}