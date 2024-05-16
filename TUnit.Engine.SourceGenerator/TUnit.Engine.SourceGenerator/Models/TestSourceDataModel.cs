using System.Collections.Generic;
using System.Linq;
using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Models;

internal record TestSourceDataModel
{
    public required string FullyQualifiedTypeName { get; init; }
    public required string MinimalTypeName { get; init; }
    public required string MethodName { get; init; }
    public required Argument[] ClassArguments { get; init; }
    public required string[] ClassParameterTypes { get; init; }

    public required Argument[] MethodArguments { get; init; }
    
    public required string[] MethodParameterTypes { get; init; }
    public required int MethodGenericTypeCount { get; init; }
    
    public required bool IsEnumerableClassArguments { get; init; }
    public required bool IsEnumerableMethodArguments { get; init; }
    
    public required string TestId { get; init; }
    public required int Order { get; init; }
    public required int RetryCount { get; init; }
    public required int RepeatIndex { get; init; }
    
    public required int CurrentMethodRepeatCount { get; init; }
    public required int CurrentClassRepeatCount { get; init; }
    
    public required string ReturnType { get; init; }
    public required string Categories { get; init; }
    public required string NotInParallelConstraintKeys { get; init; }
    public required string Timeout { get; init; }
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }
    public required IEnumerable<string> CustomProperties { get; init; }
    public required string ApplicableTestAttributes { get; init; }
    public required string BeforeEachTestInvocations { get; init; }
    public required string AfterEachTestInvocations { get; init; }

    public IEnumerable<string> GetClassArgumentVariableNames()
    {
        if (IsEnumerableClassArguments)
        {
            return ["classArg0"];
        }

        return Enumerable.Range(0, ClassArguments.Length)
            .Select(i => $"classArg{i}");
    }

    public IEnumerable<string> GetClassArgumentsInvocations()
    {
        if (IsEnumerableClassArguments)
        {
            yield return $"var classArg0 = {VariableNames.ClassData};";
            yield break;
        }
        
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
    {
        if (IsEnumerableMethodArguments)
        {
            return ["methodArg0"];
        }
        
        return Enumerable.Range(0, MethodArguments.Length)
            .Select(i => $"methodArg{i}");
    }

    public IEnumerable<string> GetMethodArgumentsInvocations()
    {
        if (IsEnumerableMethodArguments)
        {
            yield return $"var methodArg0 = {VariableNames.MethodData};";
            yield break;
        }
        
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

    public virtual bool Equals(TestSourceDataModel? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return FullyQualifiedTypeName == other.FullyQualifiedTypeName
               && MinimalTypeName == other.MinimalTypeName
               && MethodName == other.MethodName 
               && ClassParameterTypes.SequenceEqual(other.ClassParameterTypes) 
               && ClassArguments.SequenceEqual(other.ClassArguments) 
               && MethodParameterTypes.SequenceEqual(other.MethodParameterTypes) 
               && MethodArguments.SequenceEqual(other.MethodArguments) 
               && TestId == other.TestId 
               && Order == other.Order 
               && RetryCount == other.RetryCount 
               && RepeatIndex == other.RepeatIndex 
               && CurrentMethodRepeatCount == other.CurrentMethodRepeatCount 
               && CurrentClassRepeatCount == other.CurrentClassRepeatCount 
               && ReturnType == other.ReturnType 
               && Categories == other.Categories 
               && NotInParallelConstraintKeys == other.NotInParallelConstraintKeys 
               && Timeout == other.Timeout 
               && FilePath == other.FilePath 
               && LineNumber == other.LineNumber 
               && CustomProperties.SequenceEqual(other.CustomProperties) 
               && ApplicableTestAttributes == other.ApplicableTestAttributes 
               && BeforeEachTestInvocations == other.BeforeEachTestInvocations 
               && AfterEachTestInvocations == other.AfterEachTestInvocations;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = FullyQualifiedTypeName.GetHashCode();
            hashCode = (hashCode * 397) ^ MinimalTypeName.GetHashCode();
            hashCode = (hashCode * 397) ^ MethodName.GetHashCode();
            hashCode = (hashCode * 397) ^ ClassParameterTypes.GetHashCode();
            hashCode = (hashCode * 397) ^ ClassArguments.GetHashCode();
            hashCode = (hashCode * 397) ^ MethodParameterTypes.GetHashCode();
            hashCode = (hashCode * 397) ^ MethodArguments.GetHashCode();
            hashCode = (hashCode * 397) ^ TestId.GetHashCode();
            hashCode = (hashCode * 397) ^ Order;
            hashCode = (hashCode * 397) ^ RetryCount;
            hashCode = (hashCode * 397) ^ RepeatIndex;
            hashCode = (hashCode * 397) ^ CurrentMethodRepeatCount;
            hashCode = (hashCode * 397) ^ CurrentClassRepeatCount;
            hashCode = (hashCode * 397) ^ ReturnType.GetHashCode();
            hashCode = (hashCode * 397) ^ Categories.GetHashCode();
            hashCode = (hashCode * 397) ^ NotInParallelConstraintKeys.GetHashCode();
            hashCode = (hashCode * 397) ^ Timeout.GetHashCode();
            hashCode = (hashCode * 397) ^ FilePath.GetHashCode();
            hashCode = (hashCode * 397) ^ LineNumber;
            hashCode = (hashCode * 397) ^ CustomProperties.GetHashCode();
            hashCode = (hashCode * 397) ^ ApplicableTestAttributes.GetHashCode();
            hashCode = (hashCode * 397) ^ BeforeEachTestInvocations.GetHashCode();
            hashCode = (hashCode * 397) ^ AfterEachTestInvocations.GetHashCode();
            return hashCode;
        }
    }
}