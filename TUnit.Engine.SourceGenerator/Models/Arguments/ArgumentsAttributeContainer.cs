using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record ArgumentsAttributeContainer : DataAttributeContainer
{
    public Argument[] Arguments { get; init; }
    public override string[] VariableNames { get; }
    
    public ArgumentsAttributeContainer(ArgumentsType argumentsType, Argument[] arguments) : base(argumentsType)
    {
        Arguments = arguments;
        VariableNames = arguments.Select(x => GenerateUniqueVariableName()).ToArray();
    }

    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter)
    {
        for (var index = 0; index < Arguments.Length; index++)
        {
            var argument = Arguments[index];
            
            var invocation = argument.Invocation;
            
            var variableName = VariableNames[index];
            
            sourceCodeWriter.WriteLine($"{argument.Type} {variableName} = {invocation};");
        }
        
        sourceCodeWriter.WriteLine();
    }

    public override void CloseInvocationStatementsParenthesis(SourceCodeWriter sourceCodeWriter)
    {
        // Nothing
    }

    public override string[] GetArgumentTypes()
    {
        return Arguments.Select(x => x.Type).ToArray();
    }
}