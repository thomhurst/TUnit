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
    
    public required string TestId { get; init; }
    public required int CurrentRepeatAttempt { get; init; }
    
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }
    
    public required bool HasTimeoutAttribute { get; init; }
    
    public required string? CustomDisplayName { get; init; }
    public required int RepeatLimit { get; init; }

    public IEnumerable<string> GetClassArgumentVariableNames()
    {
        for (var index = 0; index < ClassArguments.Length; index++)
        {
            var classArgument = ClassArguments[index];
            if (classArgument.TupleVariableNames != null)
            {
                foreach (var tupleVariableName in classArgument.TupleVariableNames)
                {
                    yield return tupleVariableName;
                }
            }
            else
            {
                yield return $"{VariableNames.ClassArg}{index}";
            }
        }
    }

    public IEnumerable<string> GetClassArgumentsInvocations()
    {
        for (var index = 0; index < ClassArguments.Length; index++)
        {
            var argument = ClassArguments[index];
            yield return
                $"{SpecifyTypeOrVar(argument, IsEnumerableClassArguments)} {GetVariableName(argument, VariableNames.ClassArg, index)} = {GetArgumentInvocation(argument, IsEnumerableClassArguments, VariableNames.ClassData)};";
        }
    }

    private static string GetArgumentInvocation(Argument argument, bool isEnumerableArguments, string defaultEnumerableVariableName)
    {
        return isEnumerableArguments ? defaultEnumerableVariableName : argument.Invocation;
    }

    private string GetVariableName(Argument argument, string prefix, int index)
    {
        if (argument.TupleVariableNames != null)
        {
            return $"({string.Join(", ", argument.TupleVariableNames)})";
        }

        return $"{prefix}{index}";
    }

    public string GetClassArgumentVariableNamesAsList()
        => string.Join(", ", GetClassArgumentVariableNames()).TrimStart('(').TrimEnd(')');
    
    public IEnumerable<string> GetMethodArgumentVariableNames()
    {
        for (var index = 0; index < MethodArguments.Length; index++)
        {
            var methodArgument = MethodArguments[index];
            if (methodArgument.TupleVariableNames != null)
            {
                foreach (var tupleVariableName in methodArgument.TupleVariableNames)
                {
                    yield return tupleVariableName;
                }
            }
            else
            {
                yield return $"{VariableNames.MethodArg}{index}";
            }
        }
    }

    public IEnumerable<string> GetMethodArgumentsInvocations()
    {
        for (var index = 0; index < MethodArguments.Length; index++)
        {
            var argument = MethodArguments[index];
            yield return
                $"{SpecifyTypeOrVar(argument, IsEnumerableMethodArguments)} {GetVariableName(argument, VariableNames.MethodArg, index)} = {GetArgumentInvocation(argument, IsEnumerableMethodArguments, VariableNames.MethodData)};";
        }
    }
    
    public string GetCommaSeparatedMethodArgumentVariableNames()
    {
        return string.Join(", ", GetMethodArgumentVariableNames()).TrimStart('(')
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

    private string SpecifyTypeOrVar(Argument argument, bool isEnumerableArguments)
    {
        if (argument.TupleVariableNames != null || isEnumerableArguments)
        {
            return "var";
        }
            
        return argument.Type;
    }
}