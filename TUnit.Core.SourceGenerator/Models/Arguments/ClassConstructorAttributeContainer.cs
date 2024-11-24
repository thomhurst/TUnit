namespace TUnit.Core.SourceGenerator.Arguments;

public record ClassConstructorAttributeContainer(ArgumentsType ArgumentsType) : ArgumentsContainer(ArgumentsType)
{
    public required string ClassConstructorType { get; init; }

    public string Invocation => $"new {ClassConstructorType}()";

    public override void OpenScope(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
    }

    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
    }

    public override void CloseScope(SourceCodeWriter sourceCodeWriter)
    {
        // Nothing
    }
    
    public override string[] GetArgumentTypes()
    {
        return [];
    }
}