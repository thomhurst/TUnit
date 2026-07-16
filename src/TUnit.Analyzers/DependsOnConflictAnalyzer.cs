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
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol) context.Symbol;

        var dependsOnAttributes = GetDependsOnAttributes(method).Concat(GetDependsOnAttributes(method.ReceiverType ?? method.ContainingType)).ToArray();

        if (dependsOnAttributes.Length == 0)
        {
            return;
        }

        var cyclePath = new List<IMethodSymbol>();
        var visited = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

        if (FindCycleBackTo(context, method, method, dependsOnAttributes, cyclePath, visited))
        {
            var chainDescription = string.Join(" > ",
                new[] { method }.Concat(cyclePath).Concat(new[] { method })
                    .Select(x => $"{(x.ReceiverType ?? x.ContainingType).Name}.{x.Name}"));

            context.ReportDiagnostic(Diagnostic.Create(Rules.DependsOnConflicts,
                method.Locations.FirstOrDefault(), chainDescription));
        }
    }

    private AttributeData[] GetDependsOnAttributes(ISymbol methodSymbol)
    {
        var attributes = methodSymbol.GetAttributes();

        return attributes.Where(x =>
                x.AttributeClass?.IsOrInherits(WellKnown.AttributeFullyQualifiedClasses.DependsOnAttribute.WithGlobalPrefix) == true)
            .ToArray();
    }

    /// <summary>
    /// Performs a DFS from <paramref name="currentMethod"/> looking for a path back to <paramref name="targetMethod"/>.
    /// Returns true if a cycle is found, with <paramref name="path"/> containing the intermediate methods.
    /// </summary>
    private bool FindCycleBackTo(SymbolAnalysisContext context, IMethodSymbol targetMethod,
        IMethodSymbol currentMethod, AttributeData[] dependsOnAttributes,
        List<IMethodSymbol> path, HashSet<IMethodSymbol> visited)
    {
        if (!currentMethod.IsTestMethod(context.Compilation))
        {
            return false;
        }

        if (dependsOnAttributes.Length == 0)
        {
            return false;
        }

        foreach (var dependsOnAttribute in dependsOnAttributes)
        {
            var dependencyType = GetTypeContainingMethod(currentMethod, dependsOnAttribute);

            var dependencyMethodName = dependsOnAttribute.ConstructorArguments
                .FirstOrNull(x => x.Kind == TypedConstantKind.Primitive)?.Value as string;

            var dependencyParameterTypes = dependsOnAttribute.ConstructorArguments
                .FirstOrNull(x => x.Kind == TypedConstantKind.Array)
                ?.Values
                .Select(x => (INamedTypeSymbol) x.Value!)
                .ToArray();

            if (dependencyType is not INamedTypeSymbol namedTypeSymbol)
            {
                continue;
            }

            var methods = namedTypeSymbol
                .GetSelfAndBaseTypes()
                .SelectMany(x => x.GetMembers())
                .OfType<IMethodSymbol>()
                .Where(x => x.MethodKind == MethodKind.Ordinary)
                .ToArray();

            if (methods.Length == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.NoMethodFound, dependsOnAttribute.GetLocation()));
                continue;
            }

            var foundDependencies = FilterMethods(dependencyMethodName, methods, dependencyParameterTypes);

            if (foundDependencies.Length == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.NoMethodFound, dependsOnAttribute.GetLocation()));
                continue;
            }

            foreach (var foundDependency in foundDependencies)
            {
                // Found a cycle back to the target method
                if (SymbolEqualityComparer.Default.Equals(foundDependency, targetMethod))
                {
                    return true;
                }

                // Skip already-visited methods to avoid infinite recursion
                if (!visited.Add(foundDependency))
                {
                    continue;
                }

                path.Add(foundDependency);

                var nestedAttributes = GetDependsOnAttributes(foundDependency)
                    .Concat(GetDependsOnAttributes(foundDependency.ReceiverType ?? foundDependency.ContainingType))
                    .ToArray();

                if (FindCycleBackTo(context, targetMethod, foundDependency, nestedAttributes, path, visited))
                {
                    return true;
                }

                path.RemoveAt(path.Count - 1);
            }
        }

        return false;
    }

    private static ITypeSymbol GetTypeContainingMethod(IMethodSymbol methodToGetDependenciesFor, AttributeData dependsOnAttribute)
    {
        if (dependsOnAttribute.AttributeClass?.IsGenericType == true)
        {
            return dependsOnAttribute.AttributeClass!.TypeArguments.First();
        }

        return dependsOnAttribute.ConstructorArguments
                   .FirstOrNull(x => x.Kind == TypedConstantKind.Type)?.Value as INamedTypeSymbol
               ?? methodToGetDependenciesFor.ReceiverType
               ?? methodToGetDependenciesFor.ContainingType;
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
