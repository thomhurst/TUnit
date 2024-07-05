using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record TestClassTypeSharedArgument : Argument
{
    public TestClassTypeSharedArgument(ArgumentSource argumentSource, string type, string? invocation, bool isTuple = false) : base(argumentSource, type, invocation, isTuple)
    {
    }
    
    public required string TestClassType { get; init; }
}