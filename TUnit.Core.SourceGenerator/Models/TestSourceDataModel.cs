using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
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
        return TestId.GetHashCode();
    }

    public required string FullyQualifiedTypeName { get; init; }
    public required string MinimalTypeName { get; init; }
    public required string MethodName { get; init; }
    public required INamedTypeSymbol ClassMetadata { get; init; }
    public required IMethodSymbol MethodMetadata { get; init; }
    public required BaseContainer ClassArguments { get; init; }
    
    public required BaseContainer MethodArguments { get; init; }
    
    public required string[] MethodArgumentTypes { get; init; }
    
    public required int MethodGenericTypeCount { get; init; }
    
    public required string TestId { get; init; }
    public required int CurrentRepeatAttempt { get; init; }
    
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }

    public required int RepeatLimit { get; init; }
    
    public required ClassPropertiesContainer PropertyArguments { get; init; }
    public string ClassNameToGenerate => MinimalTypeName;
    public required TestGenerationContext TestGenerationContext { get; init; }
    public IDictionary<string, string>? GenericSubstitutions { get; set; }

    public string MethodVariablesWithCancellationToken()
    {
        var variableNames = MethodArguments is ArgumentsContainer argumentsContainer
            ? argumentsContainer.DataVariables.Select(x => x.Name)
            : [];
        
        if (MethodArgumentTypes.Any(type => type == WellKnownFullyQualifiedClassNames.CancellationToken.WithGlobalPrefix))
        {
            variableNames = [..variableNames, "cancellationToken"];
        }
        
        return variableNames.ToCommaSeparatedString();
    }
}