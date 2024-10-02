namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record ClassConstructorAttributeContainer : ArgumentsContainer
{
    public required string ClassConstructorType { get; init; }
    public int AttributeIndex { get; init; }

    public override void GenerateInvocationStatements(SourceCodeWriter sourceCodeWriter)
    {
        sourceCodeWriter.WriteLine($"var classConstructor = new {ClassConstructorType}();");
        sourceCodeWriter.WriteLine();
    }

    public override void CloseInvocationStatementsParenthesis(SourceCodeWriter sourceCodeWriter)
    {
        // Nothing
    }

    public override string[] GenerateArgumentVariableNames()
    {
        return [];
    }

    public override string[] GetArgumentTypes()
    {
        return [];
    }
}