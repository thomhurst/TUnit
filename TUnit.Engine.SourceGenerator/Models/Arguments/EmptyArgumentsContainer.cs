using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record EmptyArgumentsContainer(ArgumentsType ArgumentsType) : ArgumentsContainer(ArgumentsType)
{
    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter)
    {
        // Nothing
    }

    public override void CloseInvocationStatementsParenthesis(SourceCodeWriter sourceCodeWriter)
    {
        // Nothing
    }

    public override string[] VariableNames { get; } = [];

    public override string[] GetArgumentTypes()
    {
        return [];
    }
}