using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DependsOnConflictAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.DependsOnConflicts, Rules.NoMethodFound);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var methods = context.Symbol switch
        {
            IMethodSymbol method => [method],
            INamedTypeSymbol namedTypeSymbol => namedTypeSymbol.GetMembers().OfType<IMethodSymbol>().ToList(),
            _ => throw new ArgumentException()
        };

        var dependsOnAttributes = context.Symbol switch
        {
            IMethodSymbol method => GetDependsOnAttributes(method),
            INamedTypeSymbol namedTypeSymbol => GetDependsOnAttributes(namedTypeSymbol),
            _ => throw new ArgumentException()
        };
        
        foreach (var methodSymbol in methods)
        {
            var dependencies = GetDependencies(context, methodSymbol, methodSymbol, dependsOnAttributes).ToArray();

            if (!dependencies.Any() || !dependencies.Contains(methodSymbol, SymbolEqualityComparer.Default))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rules.DependsOnConflicts,
                methodSymbol.Locations.FirstOrDefault(),
                string.Join(" > ", [methodSymbol.Name, ..dependencies.Select(x => x.Name)])));
        }
    }

    private AttributeData[] GetDependsOnAttributes(ISymbol methodSymbol)
    {
        var attributes = methodSymbol.GetAttributes();

        return attributes.Where(x =>
                x.AttributeClass?.IsOrInherits(WellKnown.AttributeFullyQualifiedClasses.DependsOnAttribute) == true)
            .ToArray();
    }

    private IEnumerable<IMethodSymbol> GetDependencies(SymbolAnalysisContext context, IMethodSymbol originalMethod,
        IMethodSymbol methodToGetDependenciesFor, AttributeData[] dependsOnAttributes)
    {
        if (!methodToGetDependenciesFor.IsTestMethod())
        {
            yield break;
        }

        if (!dependsOnAttributes.Any())
        {
            yield break;
        }

        foreach (var dependsOnAttribute in dependsOnAttributes)
        {
            var dependencyType = dependsOnAttribute.ConstructorArguments
                                     .FirstOrNull(x => x.Kind == TypedConstantKind.Type)?.Value as INamedTypeSymbol
                                 ?? originalMethod.ReceiverType
                                 ?? originalMethod.ContainingType;

            var dependencyMethodName = dependsOnAttribute.ConstructorArguments
                .FirstOrDefault(x => x.Kind == TypedConstantKind.Primitive).Value as string;

            var dependencyParameterTypes = dependsOnAttribute.ConstructorArguments
                .FirstOrNull(x => x.Kind == TypedConstantKind.Array)
                ?.Values
                .Select(x => (INamedTypeSymbol)x.Value!)
                .ToArray();

            var methods = dependencyType.GetMembers().OfType<IMethodSymbol>().ToArray();

            if (!methods.Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.NoMethodFound, dependsOnAttribute.GetLocation()));

                yield break;
            }

            var foundDependencies = FilterMethods(dependencyMethodName, methods, dependencyParameterTypes);

            if (!foundDependencies.Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.NoMethodFound,
                    dependsOnAttribute.GetLocation()));
                continue;
            }

            foreach (var foundDependency in foundDependencies)
            {
                yield return foundDependency;

                if (SymbolEqualityComparer.Default.Equals(originalMethod, foundDependency))
                {
                    yield break;
                }

                foreach (var nestedDependency in GetDependencies(context, originalMethod, foundDependency, [..GetDependsOnAttributes(foundDependency), ..GetDependsOnAttributes(foundDependency.ReceiverType ?? foundDependency.ContainingType)]))
                {
                    yield return nestedDependency;

                    if (SymbolEqualityComparer.Default.Equals(originalMethod, nestedDependency))
                    {
                        yield break;
                    }
                }
            }
        }
    }

    private static IMethodSymbol[] FilterMethods(string? dependencyMethodName, IMethodSymbol[] methods,
        INamedTypeSymbol[]? dependencyParameterTypes)
    {
        if (dependencyMethodName == null)
        {
            return methods;
        }
        
        var filtered = methods.Where(x => x.Name == dependencyMethodName);

        if (dependencyParameterTypes != null)
        {
            filtered = filtered.Where(x => x.Parameters.Select(p => p.Type)
                .SequenceEqual(dependencyParameterTypes, SymbolEqualityComparer.Default));
        }
        
        return filtered.ToArray();
    }
}