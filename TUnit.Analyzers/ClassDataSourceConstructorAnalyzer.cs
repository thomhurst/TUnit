using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ClassDataSourceConstructorAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.NoAccessibleConstructor);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        context.RegisterSymbolAction(AnalyzeClass, SymbolKind.NamedType);
    }

    private void AnalyzeProperty(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IPropertySymbol propertySymbol)
        {
            return;
        }

        foreach (var attribute in propertySymbol.GetAttributes())
        {
            CheckClassDataSourceAttribute(context, attribute);
        }
    }

    private void AnalyzeMethod(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        // Check method-level attributes
        foreach (var attribute in methodSymbol.GetAttributes())
        {
            CheckClassDataSourceAttribute(context, attribute);
        }

        // Check parameter-level attributes
        foreach (var parameter in methodSymbol.Parameters)
        {
            foreach (var attribute in parameter.GetAttributes())
            {
                CheckClassDataSourceAttribute(context, attribute);
            }
        }
    }

    private void AnalyzeClass(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return;
        }

        foreach (var attribute in namedTypeSymbol.GetAttributes())
        {
            CheckClassDataSourceAttribute(context, attribute);
        }
    }

    private void CheckClassDataSourceAttribute(SymbolAnalysisContext context, AttributeData attribute)
    {
        if (attribute.AttributeClass is null)
        {
            return;
        }

        // Check if this is ClassDataSourceAttribute<T>
        var attributeClassName = attribute.AttributeClass.Name;
        var attributeFullName = attribute.AttributeClass.ToDisplayString();

        if (!attributeClassName.StartsWith("ClassDataSourceAttribute") ||
            !attributeFullName.StartsWith("TUnit.Core.ClassDataSourceAttribute<"))
        {
            return;
        }

        // Get the type argument T from ClassDataSource<T>
        if (attribute.AttributeClass is not INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: > 0 } genericAttribute)
        {
            return;
        }

        var dataSourceType = genericAttribute.TypeArguments[0];

        // Skip if the type is abstract - it can't be instantiated directly anyway
        if (dataSourceType is INamedTypeSymbol { IsAbstract: true })
        {
            return;
        }

        // Skip type parameters - they can't be validated at compile time
        if (dataSourceType is ITypeParameterSymbol)
        {
            return;
        }

        if (dataSourceType is not INamedTypeSymbol namedType)
        {
            return;
        }

        // Check if there's an accessible parameterless constructor
        if (!HasAccessibleParameterlessConstructor(namedType, context.Compilation))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.NoAccessibleConstructor,
                    attribute.GetLocation() ?? context.Symbol.Locations.FirstOrDefault(),
                    namedType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }
    }

    private static bool HasAccessibleParameterlessConstructor(INamedTypeSymbol type, Compilation compilation)
    {
        // For structs, there's always an implicit parameterless constructor
        if (type.IsValueType)
        {
            return true;
        }

        // Check for an explicit parameterless constructor
        foreach (var constructor in type.InstanceConstructors)
        {
            if (constructor.Parameters.Length == 0)
            {
                // Check if the constructor is accessible
                if (constructor.DeclaredAccessibility == Accessibility.Public ||
                    constructor.DeclaredAccessibility == Accessibility.Internal ||
                    constructor.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
