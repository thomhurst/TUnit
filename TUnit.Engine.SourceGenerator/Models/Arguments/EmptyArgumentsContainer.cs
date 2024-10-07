namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record EmptyArgumentsContainer : BaseContainer
{
    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
    }

    public override void CloseInvocationStatementsParenthesis(SourceCodeWriter sourceCodeWriter)
    {
    }

    public override string[] GetArgumentTypes()
    {
        return [];
    }
}