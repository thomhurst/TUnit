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
        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            // Built at most once per compilation, and only if some abstract test class needs it,
            // instead of walking every type in the assembly for each candidate.
            var subclassInfo = new Lazy<Dictionary<INamedTypeSymbol, ConcreteSubclassInfo>>(
                () => BuildConcreteSubclassInfo(compilationStartContext.Compilation),
                LazyThreadSafetyMode.ExecutionAndPublication);

            compilationStartContext.RegisterSymbolAction(
                symbolContext => AnalyzeSymbol(symbolContext, subclassInfo),
                SymbolKind.NamedType);
        });
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context, Lazy<Dictionary<INamedTypeSymbol, ConcreteSubclassInfo>> subclassInfo)
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

                // Check if it implements IDataSourceAttribute
                return attributeClass.AllInterfaces.Any(i =>
                    i.IsGloballyQualified(WellKnown.AttributeFullyQualifiedClasses.IDataSourceAttribute.WithGlobalPrefix));
            });
        });

        if (hasDataSourceAttributes)
        {
            // Check if there are any concrete classes that inherit from this abstract class with [InheritsTests]
            subclassInfo.Value.TryGetValue(namedTypeSymbol, out var info);

            // Only report the diagnostic if:
            // 1. There ARE concrete subclasses in the source (if none exist, this is likely a library class meant to be subclassed externally)
            // 2. None of those subclasses have [InheritsTests]
            if (info.HasAnyConcreteSubclasses && !info.HasSubclassWithInheritsTests)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rules.AbstractTestClassWithDataSources,
                    namedTypeSymbol.Locations.FirstOrDefault(),
                    namedTypeSymbol.Name)
                );
            }
        }
    }

    /// <summary>
    /// For every abstract base class of a concrete type declared in the source assembly (not referenced assemblies),
    /// records whether it has any such concrete subclass and whether any of those carries <c>[InheritsTests]</c>.
    /// </summary>
    private static Dictionary<INamedTypeSymbol, ConcreteSubclassInfo> BuildConcreteSubclassInfo(Compilation compilation)
    {
        var result = new Dictionary<INamedTypeSymbol, ConcreteSubclassInfo>(SymbolEqualityComparer.Default);

        foreach (var type in GetAllNamedTypes(compilation.Assembly.GlobalNamespace))
        {
            // Skip abstract classes
            if (type.IsAbstract)
            {
                continue;
            }

            bool? hasInheritsTests = null;

            for (var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
            {
                // Only abstract classes are ever looked up.
                if (!baseType.IsAbstract)
                {
                    continue;
                }

                hasInheritsTests ??= type.GetAttributes().Any(attr =>
                    attr.AttributeClass?.IsGloballyQualified(
                    WellKnown.AttributeFullyQualifiedClasses.InheritsTestsAttribute.WithGlobalPrefix) == true);

                result.TryGetValue(baseType, out var existing);
                result[baseType] = new ConcreteSubclassInfo(
                    HasAnyConcreteSubclasses: true,
                    HasSubclassWithInheritsTests: existing.HasSubclassWithInheritsTests || hasInheritsTests.Value);
            }
        }

        return result;
    }

    private readonly record struct ConcreteSubclassInfo(bool HasAnyConcreteSubclasses, bool HasSubclassWithInheritsTests);

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
