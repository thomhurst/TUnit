using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models.Arguments;

public record EmptyArgumentsContainer : BaseContainer
{
    public override void OpenScope(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
    }

    public override AttributeData? Attribute { get; init; } = null;

    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
    }

    public override void CloseScope(SourceCodeWriter sourceCodeWriter)
    {
    }

    public override string[] GetArgumentTypes()
    {
        return [];
    }
}