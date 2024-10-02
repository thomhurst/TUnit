using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record ArgumentsAttributeContainer : DataAttributeContainer
{
    public required Argument[] Arguments { get; init; }
    
    public override void GenerateInvocationStatements(SourceCodeWriter sourceCodeWriter)
    {
        var prefix = ArgumentsType == ArgumentsType.Method ? VariableNames.MethodArg : VariableNames.ClassArg;
        
        for (var index = 0; index < Arguments.Length; index++)
        {
            var argument = Arguments.ElementAt(index);
            
            var invocation = argument.Invocation;
            
            var variableName = $"{prefix}{index}";
            
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
        var variableName = ArgumentsType == ArgumentsType.Method ? VariableNames.MethodArg : VariableNames.ClassArg;

        return Arguments.Select((_, index) => $"{variableName}{index}").ToArray();
    }

    public override string[] GetArgumentTypes()
    {
        return Arguments.Select(x => x.Type).ToArray();
    }
}