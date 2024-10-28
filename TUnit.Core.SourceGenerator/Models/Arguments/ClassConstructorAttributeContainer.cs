using TUnit.Core.SourceGenerator.Enums;

namespace TUnit.Core.SourceGenerator.Models.Arguments;

public record ClassConstructorAttributeContainer(ArgumentsType ArgumentsType) : ArgumentsContainer(ArgumentsType)
{
    public required string ClassConstructorType { get; init; }

    public string Invocation => $"new {ClassConstructorType}()";
    
    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
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