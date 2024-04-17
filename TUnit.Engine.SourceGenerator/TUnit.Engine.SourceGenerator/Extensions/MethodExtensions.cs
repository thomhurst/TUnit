using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Extensions;

public static class MethodExtensions
{
    public static AttributeData? GetTestAttribute(this IMethodSymbol methodSymbol)
    {
        return methodSymbol.GetAttributes().FirstOrDefault(x =>
            x.AttributeClass?.BaseType?.ToDisplayString() == WellKnownFullyQualifiedClassNames.BaseTestAttribute);
    }
    
    public static AttributeData[] GetAttributesIncludingClass(this IMethodSymbol methodSymbol, INamedTypeSymbol namedTypeSymbol)
    {
        return
        [
            ..methodSymbol.GetAttributes(),
            ..namedTypeSymbol.GetAttributes()
        ];
    }
}