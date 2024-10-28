namespace TUnit.Core.SourceGenerator.Models.Arguments;

public abstract record BaseContainer
{
    public HashSet<Variable> DataAttributesVariables { get; } = [];
    public HashSet<Variable> DataVariables { get; } = [];
    public abstract void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter, ref int variableIndex);
    public abstract void CloseInvocationStatementsParenthesis(SourceCodeWriter sourceCodeWriter);
    public abstract string[] GetArgumentTypes();
}