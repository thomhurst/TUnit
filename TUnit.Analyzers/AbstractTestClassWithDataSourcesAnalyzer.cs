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
            // Check if there are any concrete classes that inherit from this abstract class with [InheritsTests]
            var hasInheritingClasses = HasConcreteInheritingClassesWithInheritsTests(context, namedTypeSymbol);

            // Only report the diagnostic if no inheriting classes are found
            if (!hasInheritingClasses)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rules.AbstractTestClassWithDataSources,
                    namedTypeSymbol.Locations.FirstOrDefault(),
                    namedTypeSymbol.Name)
                );
            }
        }
    }

    private static bool HasConcreteInheritingClassesWithInheritsTests(SymbolAnalysisContext context, INamedTypeSymbol abstractClass)
    {
        // Get all named types in the compilation
        var allTypes = GetAllNamedTypes(context.Compilation.Assembly.GlobalNamespace);

        // Check if any concrete class inherits from the abstract class and has [InheritsTests]
        foreach (var type in allTypes)
        {
            // Skip abstract classes
            if (type.IsAbstract)
            {
                continue;
            }

            // Check if this type inherits from our abstract class
            var baseType = type.BaseType;
            while (baseType != null)
            {
                if (SymbolEqualityComparer.Default.Equals(baseType, abstractClass))
                {
                    // Check if this type has [InheritsTests] attribute
                    var hasInheritsTests = type.GetAttributes().Any(attr =>
                        attr.AttributeClass?.GloballyQualified() ==
                        WellKnown.AttributeFullyQualifiedClasses.InheritsTestsAttribute.WithGlobalPrefix);

                    if (hasInheritsTests)
                    {
                        return true;
                    }

                    break;
                }

                baseType = baseType.BaseType;
            }
        }

        return false;
    }

    private static IEnumerable<INamedTypeSymbol> GetAllNamedTypes(INamespaceSymbol namespaceSymbol)
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamedTypeSymbol namedType)
            {
                yield return namedType;

                // Recursively get nested types
                foreach (var nestedType in GetNestedTypes(namedType))
                {
                    yield return nestedType;
                }
            }
            else if (member is INamespaceSymbol childNamespace)
            {
                foreach (var type in GetAllNamedTypes(childNamespace))
                {
                    yield return type;
                }
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol typeSymbol)
    {
        foreach (var member in typeSymbol.GetTypeMembers())
        {
            yield return member;

            foreach (var nestedType in GetNestedTypes(member))
            {
                yield return nestedType;
            }
        }
    }
}
