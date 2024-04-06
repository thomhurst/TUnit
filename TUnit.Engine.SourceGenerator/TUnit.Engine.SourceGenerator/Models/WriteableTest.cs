using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Models;

internal record WriteableTest
{
    public WriteableTest(IMethodSymbol MethodSymbol,
        IReadOnlyList<Argument> ClassArguments,
        IReadOnlyList<Argument> MethodArguments,
        int CurrentClassCount,
        int CurrentMethodCount)
    {
        this.MethodSymbol = MethodSymbol;
        this.ClassArguments = Map(ClassArguments);
        this.MethodArguments = Map(MethodArguments);
        this.CurrentClassCount = CurrentClassCount;
        this.CurrentMethodCount = CurrentMethodCount;
    }

    private IReadOnlyList<Argument>? Map(IReadOnlyList<Argument> arguments)
    {
        return arguments
            .Where(x => x != Argument.NoArguments)
            .Select(x => MapPrimitive(x))
            .ToList();
    }

    private Argument MapPrimitive(Argument argument)
    {
        if (argument.Type == "global::System.Char")
        {
            return argument with
            {
                Invocation = $"'{argument.Invocation}'"
            };
        }
        
        if (argument.Type == "global::System.String")
        {
            return argument with
            {
                Invocation = $"\"{argument.Invocation}\""
            };
        }
        
        return argument;
    }

    public string TestId => TestInformationGenerator.GetTestId(MethodSymbol, CurrentClassCount, CurrentMethodCount);
    public string MethodName => MethodSymbol.Name;
    public string ClassName => MethodSymbol.ContainingType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
    public IMethodSymbol MethodSymbol { get; init; }
    public IReadOnlyList<Argument> ClassArguments { get; init; }
    public IReadOnlyList<Argument> MethodArguments { get; init; }
    public int CurrentClassCount { get; init; }
    public int CurrentMethodCount { get; init; }

    public IEnumerable<string> GetClassArgumentVariableNames()
        => Enumerable.Range(0, ClassArguments.Count)
            .Select(i => $"classArg{i}");
    
    public IEnumerable<string> GetClassArgumentsInvocations()
    {
        var variableNames = GetClassArgumentVariableNames().ToList();
        for (var i = 0; i < ClassArguments.Count; i++)
        {
            var argument = ClassArguments[i];
            var variable = variableNames[i];
            yield return $"{argument.Type} {variable} = {argument.Invocation};";
        }
    }

    public string GetClassArgumentVariableNamesAsList()
        => string.Join(",", GetClassArgumentVariableNames());
    
    public IEnumerable<string> GetMethodArgumentVariableNames()
        => Enumerable.Range(0, MethodArguments.Count)
            .Select(i => $"methodArg{i}");
    
    public IEnumerable<string> GetMethodArgumentsInvocations()
    {
        var variableNames = GetMethodArgumentVariableNames().ToList();
        for (var i = 0; i < MethodArguments.Count; i++)
        {
            var argument = MethodArguments[i];
            var variable = variableNames[i];
            yield return $"{argument.Type} {variable} = {argument.Invocation};";
        }
    }
    
    public string GetMethodArgumentVariableNamesAsList()
        => string.Join(",", GetMethodArgumentVariableNames());

    public void Deconstruct(out IMethodSymbol MethodSymbol, out IReadOnlyList<Argument> ClassArguments, out IReadOnlyList<Argument> MethodArguments, out int CurrentClassCount, out int CurrentMethodCount)
    {
        MethodSymbol = this.MethodSymbol;
        ClassArguments = this.ClassArguments;
        MethodArguments = this.MethodArguments;
        CurrentClassCount = this.CurrentClassCount;
        CurrentMethodCount = this.CurrentMethodCount;
    }
}