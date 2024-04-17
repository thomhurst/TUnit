using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

namespace TUnit.Engine.SourceGenerator.Models;

internal record WriteableTest
{
    public WriteableTest(IMethodSymbol MethodSymbol,
        INamedTypeSymbol ClassSymbol,
        IReadOnlyList<Argument> ClassArguments,
        IReadOnlyList<Argument> MethodArguments,
        int CurrentClassRepeatCount,
        int CurrentMethodRepeatCount)
    {
        this.MethodSymbol = MethodSymbol;
        this.ClassSymbol = ClassSymbol;
        this.ClassArguments = Map(ClassArguments);
        this.MethodArguments = Map(MethodArguments);
        this.CurrentClassRepeatCount = CurrentClassRepeatCount;
        this.CurrentMethodRepeatCount = CurrentMethodRepeatCount;
    }

    private IReadOnlyList<Argument> Map(IEnumerable<Argument> arguments)
    {
        return arguments
            .Where(x => x != Argument.NoArguments)
            .ToList();
    }

    public string TestId => TestInformationRetriever.GetTestId(ClassSymbol, MethodSymbol, CurrentClassRepeatCount, CurrentMethodRepeatCount);
    public string MethodName => MethodSymbol.Name;
    public string ClassName => ClassSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
    public IMethodSymbol MethodSymbol { get; init; }
    public INamedTypeSymbol ClassSymbol { get; }
    public IReadOnlyList<Argument> ClassArguments { get; init; }
    public IReadOnlyList<Argument> MethodArguments { get; init; }
    public int CurrentClassRepeatCount { get; init; }
    public int CurrentMethodRepeatCount { get; init; }

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
}