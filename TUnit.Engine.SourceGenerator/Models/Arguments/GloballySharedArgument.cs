namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record GloballySharedArgument : Argument
{
    public GloballySharedArgument(string type, string? invocation, bool isTuple = false) : base(type, invocation, isTuple)
    {
    }
}