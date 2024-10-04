using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record EmptyArgumentsContainer(ArgumentsType ArgumentsType) : ArgumentsContainer(ArgumentsType)
{
    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
        // Nothing
    }

    public override void CloseInvocationStatementsParenthesis(SourceCodeWriter sourceCodeWriter)
    {
        // Nothing
    }
    
    public override string[] GetArgumentTypes()
    {
        return [];
    }
}