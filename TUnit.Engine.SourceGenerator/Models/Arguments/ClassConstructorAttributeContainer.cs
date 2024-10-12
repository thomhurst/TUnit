using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record ClassConstructorAttributeContainer(ArgumentsType ArgumentsType) : ArgumentsContainer(ArgumentsType)
{
    public required string ClassConstructorType { get; init; }

    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
        sourceCodeWriter.WriteLine(GenerateVariable(ClassConstructorType, $"new {ClassConstructorType}()", ref variableIndex).ToString());
        sourceCodeWriter.WriteLine();
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