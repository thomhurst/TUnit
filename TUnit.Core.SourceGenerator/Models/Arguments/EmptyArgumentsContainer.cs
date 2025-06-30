using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models.Arguments;

public record EmptyArgumentsContainer : BaseContainer
{
    public override void OpenScope(ICodeWriter sourceCodeWriter, ref int variableIndex)
    {
    }

    public override AttributeData? Attribute { get; init; } = null;

    public override void WriteVariableAssignments(ICodeWriter sourceCodeWriter, ref int variableIndex)
    {
    }

    public override void CloseScope(ICodeWriter sourceCodeWriter)
    {
    }

    public override string[] GetArgumentTypes()
    {
        return [];
    }
}
