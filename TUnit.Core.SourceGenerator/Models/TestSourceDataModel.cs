using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models.Arguments;

namespace TUnit.Core.SourceGenerator.Models;

public record TestSourceDataModel
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
            hashCode = (hashCode * 397) ^ TestId.GetHashCode();
            hashCode = (hashCode * 397) ^ CurrentRepeatAttempt;
            hashCode = (hashCode * 397) ^ FilePath.GetHashCode();
            hashCode = (hashCode * 397) ^ LineNumber;
            hashCode = (hashCode * 397) ^ RepeatLimit;
            return hashCode;
        }
    }

    public required string FullyQualifiedTypeName { get; init; }
    public required string MinimalTypeName { get; init; }
    public required string MethodName { get; init; }
    public required BaseContainer ClassArguments { get; init; }
    
    public required string[] ClassParameterOrArgumentNonGenericTypes { get; init; }

    public required BaseContainer MethodArguments { get; init; }
    
    public required string[] MethodParameterTypes { get; init; }
    public required string[] MethodParameterNames { get; init; }
    public required string[] MethodParameterOrArgumentNonGenericTypes { get; init; }

    public required int MethodGenericTypeCount { get; init; }
    
    public required string TestId { get; init; }
    public required int CurrentRepeatAttempt { get; init; }
    
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }

    public required int RepeatLimit { get; init; }
    public required string? TestExecutor { get; init; }
    public required string[] AttributeTypes { get; init; }
    public required string[] PropertyAttributeTypes { get; init; }
    public required ClassPropertiesContainer PropertyArguments { get; init; }
    public required string AssemblyName { get; init; }
    public required string Namespace { get; init; }

    public string ClassNameToGenerate => new string([..MinimalTypeName, '_', ..Namespace, '_', ..AssemblyName]).Replace('.', '_');

    public string MethodVariablesWithCancellationToken()
    {
        var variableNames = MethodArguments is ArgumentsContainer argumentsContainer
            ? argumentsContainer.DataVariables.Select(x => x.Name)
            : [];
        
        if (MethodParameterTypes.Any(type => type == WellKnownFullyQualifiedClassNames.CancellationToken.WithGlobalPrefix))
        {
            variableNames = [..variableNames, "cancellationToken"];
        }
        
        return variableNames.ToCommaSeparatedString();
    }
}