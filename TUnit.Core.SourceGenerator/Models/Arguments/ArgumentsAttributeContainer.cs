using TUnit.Core.SourceGenerator.Enums;

namespace TUnit.Core.SourceGenerator.Models.Arguments;

public record ArgumentsAttributeContainer(ArgumentsType ArgumentsType, Argument[] Arguments)
    : ArgumentsContainer(ArgumentsType)
{
    public override void OpenScope(ICodeWriter sourceCodeWriter, ref int variableIndex)
    {
    }

    public override void WriteVariableAssignments(ICodeWriter sourceCodeWriter, ref int variableIndex)
    {
        foreach (var argument in Arguments)
        {
            sourceCodeWriter.Append(GenerateVariable(argument.Type, argument.Invocation, ref variableIndex).ToString());
        }

        sourceCodeWriter.AppendLine();
    }

    public override void CloseScope(ICodeWriter sourceCodeWriter)
    {
        // Nothing
    }

    public override string[] GetArgumentTypes()
    {
        return Arguments.Select(x => x.Type).ToArray();
    }
}
