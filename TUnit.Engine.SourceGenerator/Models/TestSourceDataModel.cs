﻿using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.Models;

internal record TestSourceDataModel
{
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

        return TestId == other.TestId;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = FullyQualifiedTypeName.GetHashCode();
            hashCode = (hashCode * 397) ^ MinimalTypeName.GetHashCode();
            hashCode = (hashCode * 397) ^ MethodName.GetHashCode();
            hashCode = (hashCode * 397) ^ ClassArguments.GetHashCode();
            hashCode = (hashCode * 397) ^ MethodArguments.GetHashCode();
            hashCode = (hashCode * 397) ^ MethodParameterTypes.GetHashCode();
            hashCode = (hashCode * 397) ^ MethodParameterNames.GetHashCode();
            hashCode = (hashCode * 397) ^ MethodGenericTypeCount;
            hashCode = (hashCode * 397) ^ IsEnumerableClassArguments.GetHashCode();
            hashCode = (hashCode * 397) ^ IsEnumerableMethodArguments.GetHashCode();
            hashCode = (hashCode * 397) ^ TestId.GetHashCode();
            hashCode = (hashCode * 397) ^ CurrentRepeatAttempt;
            hashCode = (hashCode * 397) ^ FilePath.GetHashCode();
            hashCode = (hashCode * 397) ^ LineNumber;
            hashCode = (hashCode * 397) ^ HasTimeoutAttribute.GetHashCode();
            hashCode = (hashCode * 397) ^ (CustomDisplayName != null ? CustomDisplayName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ RepeatLimit;
            return hashCode;
        }
    }

    public required string FullyQualifiedTypeName { get; init; }
    public required string MinimalTypeName { get; init; }
    public required string MethodName { get; init; }
    public required Argument[] ClassArguments { get; init; }

    public required Argument[] MethodArguments { get; init; }
    
    public required string[] MethodParameterTypes { get; init; }
    public required string[] MethodParameterNames { get; init; }
    public required int MethodGenericTypeCount { get; init; }
    
    public required bool IsEnumerableClassArguments { get; init; }
    public required bool IsEnumerableMethodArguments { get; init; }
    
    public required string[] ClassDataInvocations { get; init; }
    public required string[] ClassVariables { get; init; }
    
    public required string[] MethodDataInvocations { get; init; }
    public required string[] MethodVariables { get; init; }
    
    public required string TestId { get; init; }
    public required int CurrentRepeatAttempt { get; init; }
    
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }
    
    public required bool HasTimeoutAttribute { get; init; }
    
    public required string? CustomDisplayName { get; init; }
    public required int RepeatLimit { get; init; }
    public required string? TestExecutor { get; init; }
    public required string? ParallelLimit { get; init; }
    public required string[] AttributeTypes { get; init; }
    public required string? ClassConstructorCommand { get; set; }

    public string MethodVariablesWithCancellationToken()
    {
        var variableNamesAsList = MethodVariables.ToCommaSeparatedString();

        if (HasTimeoutAttribute)
        {
            return string.IsNullOrEmpty(variableNamesAsList) ? "cancellationToken" : $"{variableNamesAsList}, cancellationToken";
        }
        
        return variableNamesAsList;
    }
}