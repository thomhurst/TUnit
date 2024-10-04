using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal abstract record ArgumentsContainer(ArgumentsType ArgumentsType)
{
    public required bool DisposeAfterTest { get; init; }
    public abstract void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter, ref int variableIndex);
    public abstract void CloseInvocationStatementsParenthesis(SourceCodeWriter sourceCodeWriter);
    public List<string> VariableNames { get; } = [];
    public abstract string[] GetArgumentTypes();

    protected string VariableNamePrefix
    {
        get
        {
            return ArgumentsType switch
            {
                ArgumentsType.ClassConstructor => CodeGenerators.VariableNames.ClassArg,
                ArgumentsType.Property => CodeGenerators.VariableNames.PropertyArg,
                _ => CodeGenerators.VariableNames.MethodArg
            };
        }
    }

    protected string GenerateVariableName(ref int index)
    {
        if (index == 0)
        {
            index++;
            return AddVariable(VariableNamePrefix);
        }
        
        return AddVariable($"{VariableNamePrefix}{index++}");
    }

    protected string AddVariable(string variableName)
    {
        VariableNames.Add(variableName);
        return variableName;
    }
};