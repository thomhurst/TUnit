using TUnit.Core.SourceGenerator.Enums;

namespace TUnit.Core.SourceGenerator.Models.Arguments;

public record ArgumentsAttributeContainer(ArgumentsType ArgumentsType, Argument[] Arguments)
    : ArgumentsContainer(ArgumentsType)
{
    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
        foreach (var argument in Arguments)
        {
            sourceCodeWriter.WriteLine(GenerateVariable(argument.Type, argument.Invocation, ref variableIndex).ToString());
        }

        sourceCodeWriter.WriteLine();
    }

    public override void CloseInvocationStatementsParenthesis(SourceCodeWriter sourceCodeWriter)
    {
        // Nothing
    }

    public override string[] GetArgumentTypes()
    {
        return Arguments.Select(x => x.Type).ToArray();
    }
}