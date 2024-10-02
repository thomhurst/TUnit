namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record EmptyArgumentsContainer : ArgumentsContainer
{
    public override void GenerateInvocationStatements(SourceCodeWriter sourceCodeWriter)
    {
        // Nothing
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