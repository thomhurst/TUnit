using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.Models;

internal record WriteableTest(
    string TestId,
    string ClassName,
    IReadOnlyList<string> ClassArguments,
    string MethodName,
    IReadOnlyList<string> MethodArguments,
    int CurrentCount,
    IMethodSymbol MethodSymbol
)
{
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
            yield return $"var {variable} = {argument};";
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
            yield return $"var {variable} = {argument};";
        }
    }
    
    public string GetMethodArgumentVariableNamesAsList()
        => string.Join(",", GetMethodArgumentVariableNames());
}