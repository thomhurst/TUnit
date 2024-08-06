namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record TestClassTypeSharedArgument : Argument
{
    public TestClassTypeSharedArgument(string type, string? invocation, bool isTuple = false) : base(type, invocation, isTuple)
    {
    }
    
    public required string TestClassType { get; init; }
}