using System.Collections.Generic;
using TUnit.Engine.SourceGenerator.Enums;
using System.Linq;

namespace TUnit.Engine.SourceGenerator.Models;

internal record TestSourceDataModel
{
    public required string FullyQualifiedTypeName { get; init; }
    public required string MinimalTypeName { get; init; }
    public required string MethodName { get; init; }
    public required HookType HookType { get; init; }
    public required KnownArguments KnownArguments { get; init; }
    
    public required Argument[] ClassArguments { get; init; }
    public required Argument[] MethodArguments { get; init; }
    
    public required string TestId { get; init; }
    public required int Order { get; init; }
    public required int RetryCount { get; init; }
    public required int RepeatCount { get; init; }
    
    public required int CurrentMethodRepeatCount { get; init; }
    public required int CurrentClassRepeatCount { get; init; }
    
    public required string ReturnType { get; init; }
    public required string Categories { get; init; }
    public required string NotInParallelConstraintKeys { get; init; }
    public required string Timeout { get; init; }
    
    public IEnumerable<string> GetClassArgumentVariableNames()
        => Enumerable.Range(0, ClassArguments.Length)
            .Select(i => $"classArg{i}");
    
    public IEnumerable<string> GetClassArgumentsInvocations()
    {
        var variableNames = GetClassArgumentVariableNames().ToList();
        for (var i = 0; i < ClassArguments.Length; i++)
        {
            var argument = ClassArguments[i];
            var variable = variableNames[i];
            yield return $"{argument.Type} {variable} = {argument.Invocation};";
        }
    }

    public string GetClassArgumentVariableNamesAsList()
        => string.Join(",", GetClassArgumentVariableNames());
    
    public IEnumerable<string> GetMethodArgumentVariableNames()
        => Enumerable.Range(0, MethodArguments.Length)
            .Select(i => $"methodArg{i}");
    
    public IEnumerable<string> GetMethodArgumentsInvocations()
    {
        var variableNames = GetMethodArgumentVariableNames().ToList();
        for (var i = 0; i < MethodArguments.Length; i++)
        {
            var argument = MethodArguments[i];
            var variable = variableNames[i];
            yield return $"{argument.Type} {variable} = {argument.Invocation};";
        }
    }
    
    public string GetMethodArgumentVariableNamesAsList()
        => string.Join(",", GetMethodArgumentVariableNames());
}