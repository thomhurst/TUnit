using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record ArgumentsAttributeContainer : DataAttributeContainer
{
    public required Argument[] Arguments { get; init; }
    
    public override void GenerateInvocationStatements(SourceCodeWriter sourceCodeWriter)
    {
        for (var index = 0; index < Arguments.Length; index++)
        {
            var argument = Arguments.ElementAt(index);
            
            var invocation = argument.Invocation;
            
            var variableName = $"{VariableNamePrefix}{index}";
            
            sourceCodeWriter.WriteLine($"{argument.Type} {variableName} = {invocation};");
        }
        
        sourceCodeWriter.WriteLine();
    }

    public override void CloseInvocationStatementsParenthesis(SourceCodeWriter sourceCodeWriter)
    {
        // Nothing
    }

    public override string[] GenerateArgumentVariableNames()
    {
        return Arguments.Select((_, index) => $"{VariableNamePrefix}{index}").ToArray();
    }

    public override string[] GetArgumentTypes()
    {
        return Arguments.Select(x => x.Type).ToArray();
    }
}