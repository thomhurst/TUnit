using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record ClassConstructorAttributeContainer : ArgumentsContainer
{
    public required string ClassConstructorType { get; init; }

    public ClassConstructorAttributeContainer(ArgumentsType argumentsType) : base(argumentsType)
    {
    }

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