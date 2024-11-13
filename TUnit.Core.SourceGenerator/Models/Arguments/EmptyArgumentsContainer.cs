namespace TUnit.Core.SourceGenerator.Models.Arguments;

public record EmptyArgumentsContainer : BaseContainer
{
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