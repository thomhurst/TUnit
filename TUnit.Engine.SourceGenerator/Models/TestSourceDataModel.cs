using TUnit.Engine.SourceGenerator.CodeGenerators;
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

        return FullyQualifiedTypeName == other.FullyQualifiedTypeName && MinimalTypeName == other.MinimalTypeName &&
               MethodName == other.MethodName && ClassArguments.SequenceEqual(other.ClassArguments) &&
               MethodArguments.SequenceEqual(other.MethodArguments) &&
               MethodParameterTypes.SequenceEqual(other.MethodParameterTypes) &&
               MethodParameterNames.SequenceEqual(other.MethodParameterNames) &&
               MethodGenericTypeCount == other.MethodGenericTypeCount &&
               IsEnumerableClassArguments == other.IsEnumerableClassArguments &&
               IsEnumerableMethodArguments == other.IsEnumerableMethodArguments && TestId == other.TestId &&
               CurrentRepeatAttempt == other.CurrentRepeatAttempt && FilePath == other.FilePath &&
               LineNumber == other.LineNumber && BeforeEachTestInvocations == other.BeforeEachTestInvocations &&
               AfterEachTestInvocations == other.AfterEachTestInvocations &&
               HasTimeoutAttribute == other.HasTimeoutAttribute && CustomDisplayName == other.CustomDisplayName &&
               RepeatLimit == other.RepeatLimit;
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
            hashCode = (hashCode * 397) ^ BeforeEachTestInvocations.GetHashCode();
            hashCode = (hashCode * 397) ^ AfterEachTestInvocations.GetHashCode();
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
    
    public required string TestId { get; init; }
    public required int CurrentRepeatAttempt { get; init; }
    
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }
    public required string BeforeEachTestInvocations { get; init; }
    public required string AfterEachTestInvocations { get; init; }
    
    public required bool HasTimeoutAttribute { get; init; }
    
    public bool IsClassTupleArguments => ClassArguments.Any(x => x.IsTuple);

    public bool IsMethodTupleArguments => MethodArguments.Any(x => x.IsTuple);
    
    public required string? CustomDisplayName { get; init; }
    public required int RepeatLimit { get; init; }

    public IEnumerable<string> GetClassArgumentVariableNames()
    {
        return Enumerable.Range(0, ClassArguments.Length)
            .Select(i => ClassArguments[i].TupleVariableNames ?? $"{VariableNames.ClassArg}{i}");
    }

    public IEnumerable<string> GetClassArgumentsInvocations()
    {
        if (IsEnumerableClassArguments && !IsClassTupleArguments)
        {
            yield return $"var {VariableNames.ClassArg}0 = {VariableNames.ClassData};";
            yield break;
        }
        
        if (IsEnumerableClassArguments && IsClassTupleArguments)
        {
            yield return $"var {VariableNames.ClassArg}0 = {VariableNames.ClassData};";
            yield return $"var {ClassArguments[1].TupleVariableNames} = {VariableNames.ClassArg}0;";
            yield break;
        }
        
        var variableNames = GetClassArgumentVariableNames().ToList();
        for (var i = 0; i < ClassArguments.Length; i++)
        {
            var argument = ClassArguments[i];
            var variable = variableNames[i];
            yield return $"{SpecifyTypeOrVar(argument)} {variable} = {argument.Invocation};";
        }
    }

    public string GetClassArgumentVariableNamesAsList()
        => string.Join(", ", GetClassArgumentVariableNames().Skip(IsClassTupleArguments ? 1 : 0)).TrimStart('(').TrimEnd(')');
    
    public IEnumerable<string> GetMethodArgumentVariableNames()
    {
        return Enumerable.Range(0, MethodArguments.Length)
            .Select(i => MethodArguments[i].TupleVariableNames ?? $"{VariableNames.MethodArg}{i}");
    }

    public IEnumerable<string> GetMethodArgumentsInvocations()
    {
        if (IsEnumerableMethodArguments && !IsMethodTupleArguments)
        {
            yield return $"var {VariableNames.MethodArg}0 = {VariableNames.MethodData};";
            yield break;
        }
        
        if (IsEnumerableMethodArguments && IsMethodTupleArguments)
        {
            yield return $"var {VariableNames.MethodArg}0 = {VariableNames.MethodData};";
            yield return $"var {MethodArguments[1].TupleVariableNames} = {VariableNames.MethodArg}0;";
            yield break;
        }
        
        var variableNames = GetMethodArgumentVariableNames().ToList();
        for (var i = 0; i < MethodArguments.Length; i++)
        {
            var argument = MethodArguments[i];
            
            var variable = variableNames[i];
            yield return $"{SpecifyTypeOrVar(argument)} {variable} = {argument.Invocation};";
        }
    }
    
    public string GetCommaSeparatedMethodArgumentVariableNames()
    {
        return string.Join(", ", GetMethodArgumentVariableNames().Skip(IsMethodTupleArguments ? 1 : 0)).TrimStart('(')
            .TrimEnd(')');
    }
    
    public string GetCommaSeparatedMethodArgumentVariableNamesWithCancellationToken()
    {
        var variableNamesAsList = GetCommaSeparatedMethodArgumentVariableNames();

        if (HasTimeoutAttribute)
        {
            return string.IsNullOrEmpty(variableNamesAsList) ? "cancellationToken" : $"{variableNamesAsList}, cancellationToken";
        }
        
        return variableNamesAsList;
    }

    private string SpecifyTypeOrVar(Argument argument)
    {
        if (argument.TupleVariableNames != null)
        {
            return "var";
        }
            
        return argument.Type;
    }
}