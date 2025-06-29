using TUnit.Core.SourceGenerator.Enums;

namespace TUnit.Core.SourceGenerator.Models.Arguments;

public record ClassConstructorAttributeContainer(ArgumentsType ArgumentsType) : ArgumentsContainer(ArgumentsType)
{
    public required string ClassConstructorType { get; init; }

    public string Invocation => $"new {ClassConstructorType}()";

    public override void OpenScope(ICodeWriter sourceCodeWriter, ref int variableIndex)
    {
    }

    public override void WriteVariableAssignments(ICodeWriter sourceCodeWriter, ref int variableIndex)
    {
    }

    public override void CloseScope(ICodeWriter sourceCodeWriter)
    {
        // Nothing
    }

    public override string[] GetArgumentTypes()
    {
        return [];
    }
}
