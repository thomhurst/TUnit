namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record TestClassTypeSharedArgument : Argument
{
    public TestClassTypeSharedArgument(string type, string? invocation, bool isUnfoldableTuple = false) : base(type, invocation, isUnfoldableTuple)
    {
    }
    
    public required string TestClassType { get; init; }
}