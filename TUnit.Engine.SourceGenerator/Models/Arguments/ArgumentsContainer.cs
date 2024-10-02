using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal abstract record ArgumentsContainer
{
    public required ArgumentsType ArgumentsType { get; init; }
    public required bool DisposeAfterTest { get; init; }
    public abstract void GenerateInvocationStatements(SourceCodeWriter sourceCodeWriter);
    public abstract void CloseInvocationStatementsParenthesis(SourceCodeWriter sourceCodeWriter);
    public abstract string[] GenerateArgumentVariableNames();
    public abstract string[] GetArgumentTypes();

    protected string VariableNamePrefix => ArgumentsType == ArgumentsType.ClassConstructor
        ? VariableNames.ClassArg
        : VariableNames.MethodArg;
};