using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AbstractTestClassWithDataSourcesAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.AbstractTestClassWithDataSources);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return;
        }

        // Only analyze abstract classes
        if (!namedTypeSymbol.IsAbstract)
        {
            return;
        }

        // Check if it's a test class
        if (!namedTypeSymbol.IsTestClass(context.Compilation))
        {
            return;
        }

        // Get all test methods in this class
        var testMethods = namedTypeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.IsTestMethod(context.Compilation))
            .ToList();

        if (!testMethods.Any())
        {
            return;
        }

        // Check if any test method has a data source attribute
        var hasDataSourceAttributes = testMethods.Any(method =>
        {
            var attributes = method.GetAttributes();
            return attributes.Any(attr =>
            {
                var attributeClass = attr.AttributeClass;
                if (attributeClass == null)
                {
                    return false;
                }

                // Check for data source attributes
                var currentType = attributeClass;
                while (currentType != null)
                {
                    var typeName = currentType.Name;

                    // Check for known data source attributes
                    if (typeName.Contains("DataSource") || typeName == "ArgumentsAttribute")
                    {
                        return true;
                    }

                    currentType = currentType.BaseType;
                }

                // Also check if it implements IDataSourceAttribute
                return attributeClass.AllInterfaces.Any(i =>
                    i.GloballyQualified() == WellKnown.AttributeFullyQualifiedClasses.IDataSourceAttribute.WithGlobalPrefix);
            });
        });

        if (hasDataSourceAttributes)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rules.AbstractTestClassWithDataSources,
                namedTypeSymbol.Locations.FirstOrDefault(),
                namedTypeSymbol.Name)
            );
        }
    }
}
