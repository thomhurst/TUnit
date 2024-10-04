using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record ClassConstructorAttributeContainer : ArgumentsContainer
{
    public required string ClassConstructorType { get; init; }
    public int AttributeIndex { get; init; }

    public ClassConstructorAttributeContainer(ArgumentsType argumentsType) : base(argumentsType)
    {
        if (ArgumentsType == ArgumentsType.Property)
        {
            AddVariable($"classConstructor{Guid.NewGuid():N}");
        }
    }

    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
        sourceCodeWriter.WriteLine($"var {VariableNames.ElementAtOrDefault(0) ?? "classConstructor"} = new {ClassConstructorType}();");
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