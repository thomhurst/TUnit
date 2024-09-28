namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record TestClassTypeSharedArgument : Argument
{
    public TestClassTypeSharedArgument(string type, string? invocation) : base(type, invocation)
    {
    }
    
    public required string TestClassType { get; init; }
}