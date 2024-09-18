namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record GloballySharedArgument : Argument
{
    public GloballySharedArgument(string type, string? invocation, bool isUnfoldableTuple = false) : base(type, invocation, isUnfoldableTuple)
    {
    }
}