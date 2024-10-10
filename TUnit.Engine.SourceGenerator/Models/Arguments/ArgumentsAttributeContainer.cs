using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record ArgumentsAttributeContainer : ArgumentsContainer
{
    public Argument[] Arguments { get; }
    public ArgumentsAttributeContainer(ArgumentsType argumentsType, Argument[] arguments) : base(argumentsType)
    {
        Arguments = arguments;
    }

    public override void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter, ref int variableIndex)
    {
        for (var index = 0; index < Arguments.Length; index++)
        {
            var argument = Arguments[index];
            
            var invocation = argument.Invocation;
            
            sourceCodeWriter.WriteLine($"{argument.Type} {GenerateVariableName(ref variableIndex)} = {invocation};");
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