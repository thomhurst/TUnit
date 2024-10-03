using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal abstract record ArgumentsContainer(ArgumentsType ArgumentsType)
{
    public required bool DisposeAfterTest { get; init; }
    public abstract void WriteVariableAssignments(SourceCodeWriter sourceCodeWriter);
    public abstract void CloseInvocationStatementsParenthesis(SourceCodeWriter sourceCodeWriter);
    public abstract string[] VariableNames { get; }
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

    protected string GenerateVariableName(int index) => $"{VariableNamePrefix}{index}";
};