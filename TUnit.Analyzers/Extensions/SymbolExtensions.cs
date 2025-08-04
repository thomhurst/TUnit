using Microsoft.CodeAnalysis;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers.Extensions;

public static class SymbolExtensions
{
    public static bool HasDataDrivenAttributes(this ISymbol symbol)
    {
        var attributes = symbol.GetAttributes();

        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass == null)
                continue;

            // Check if the attribute directly implements IDataSourceAttribute
            if (ImplementsIDataSourceAttribute(attribute.AttributeClass))
                return true;

            // Check for well-known data source attributes as a fallback
            if (IsWellKnownDataSourceAttribute(attribute.AttributeClass))
                return true;
        }

        return HasMatrixValues(symbol);
    }

    private static bool ImplementsIDataSourceAttribute(INamedTypeSymbol attributeClass)
    {
        var expectedInterface = WellKnown.AttributeFullyQualifiedClasses.IDataSourceAttribute.WithGlobalPrefix;
        
        // Check if the class itself is the interface
        if (attributeClass.GloballyQualified() == expectedInterface)
            return true;

        // Check if any of the implemented interfaces match
        return attributeClass.AllInterfaces.Any(x => x.GloballyQualified() == expectedInterface);
    }

    private static bool IsWellKnownDataSourceAttribute(INamedTypeSymbol attributeClass)
    {
        var fullyQualifiedName = attributeClass.GloballyQualifiedNonGeneric();
        
        // List of well-known data source attributes
        return fullyQualifiedName == WellKnown.AttributeFullyQualifiedClasses.Arguments.WithGlobalPrefix ||
               fullyQualifiedName == WellKnown.AttributeFullyQualifiedClasses.MethodDataSource.WithGlobalPrefix ||
               fullyQualifiedName == WellKnown.AttributeFullyQualifiedClasses.ClassDataSource.WithGlobalPrefix ||
               fullyQualifiedName == WellKnown.AttributeFullyQualifiedClasses.MatrixDataSourceAttribute.WithGlobalPrefix;
    }

    private static bool HasMatrixValues(ISymbol symbol)
    {
        var parameters = symbol switch
        {
            INamedTypeSymbol namedTypeSymbol => namedTypeSymbol.InstanceConstructors.FirstOrDefault()?.Parameters,
            IMethodSymbol methodSymbol => methodSymbol.Parameters,
            _ => null
        };

        if (parameters == null || parameters.Value.IsDefaultOrEmpty)
        {
            return false;
        }

        return parameters.Value.Any(p => p.GetAttributes().Any(a =>
            a.AttributeClass?.GloballyQualifiedNonGeneric() ==
            WellKnown.AttributeFullyQualifiedClasses.Matrix.WithGlobalPrefix));
    }
}
