using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

public record Chain(IMethodSymbol OriginalMethod)
{
    public List<IMethodSymbol> Dependencies { get; } = [];
    
    public bool MethodTraversed(IMethodSymbol method) => Dependencies.Contains(method, SymbolEqualityComparer.Default);
    
    public bool Any() => Dependencies.Any();

    public void Add(IMethodSymbol dependency)
    {
        Dependencies.Add(dependency);
    }

    public IMethodSymbol[] GetCompleteChain()
    {
        return
        [
            OriginalMethod,
            ..Dependencies.TakeUntil(d => SymbolEqualityComparer.Default.Equals(d, OriginalMethod))
        ];
    }
}

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
        
        AttributeData[] dependsOnAttributes = [..GetDependsOnAttributes(method), ..GetDependsOnAttributes(method.ReceiverType ?? method.ContainingType)];
        
        var dependencies = GetDependencies(context, new Chain(method), method, dependsOnAttributes);

        if (!dependencies.Any() || !dependencies.MethodTraversed(method))
        {
            return;
        }
        
        context.ReportDiagnostic(Diagnostic.Create(Rules.DependsOnConflicts,
            method.Locations.FirstOrDefault(),
                string.Join(" > ", [..dependencies.GetCompleteChain().Select(x => $"{(x.ReceiverType ?? x.ContainingType).Name}.{x.Name}")])));
    }

    private AttributeData[] GetDependsOnAttributes(ISymbol methodSymbol)
    {
        var attributes = methodSymbol.GetAttributes();

        return attributes.Where(x =>
                x.AttributeClass?.IsOrInherits(WellKnown.AttributeFullyQualifiedClasses.DependsOnAttribute.WithGlobalPrefix) == true)
            .ToArray();
    }

    private Chain GetDependencies(SymbolAnalysisContext context, Chain chain,
        IMethodSymbol methodToGetDependenciesFor, AttributeData[] dependsOnAttributes)
    {
        if (!methodToGetDependenciesFor.IsTestMethod(context.Compilation))
        {
            return chain;
        }

        if (!dependsOnAttributes.Any())
        {
            return chain;
        }
        
        foreach (var dependsOnAttribute in dependsOnAttributes)
        {
            var dependencyType = dependsOnAttribute.ConstructorArguments
                                     .FirstOrNull(x => x.Kind == TypedConstantKind.Type)?.Value as INamedTypeSymbol
                                 ?? methodToGetDependenciesFor.ReceiverType
                                 ?? methodToGetDependenciesFor.ContainingType;

            var dependencyMethodName = dependsOnAttribute.ConstructorArguments
                .FirstOrNull(x => x.Kind == TypedConstantKind.Primitive)?.Value as string;

            var dependencyParameterTypes = dependsOnAttribute.ConstructorArguments
                .FirstOrNull(x => x.Kind == TypedConstantKind.Array)
                ?.Values
                .Select(x => (INamedTypeSymbol)x.Value!)
                .ToArray();

            var methods = dependencyType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(x => x.MethodKind == MethodKind.Ordinary)
                .ToArray();

            if (!methods.Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.NoMethodFound, dependsOnAttribute.GetLocation()));

                return chain;
            }

            var foundDependencies = FilterMethods(dependencyMethodName, methods, dependencyParameterTypes);

            if (!foundDependencies.Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.NoMethodFound, dependsOnAttribute.GetLocation()));
                return chain;
            }

            foreach (var foundDependency in foundDependencies)
            {
                if (chain.MethodTraversed(foundDependency))
                {
                    chain.Add(foundDependency);
                    return chain;
                }
                
                chain.Add(foundDependency);

                var nestedChain = GetDependencies(context, chain, foundDependency, [..GetDependsOnAttributes(foundDependency), ..GetDependsOnAttributes(foundDependency.ReceiverType ?? foundDependency.ContainingType)]);
                
                foreach (var nestedDependency in nestedChain.Dependencies)
                {
                    if (chain.MethodTraversed(nestedDependency))
                    {
                        chain.Add(nestedDependency);
                        return chain;
                    }
                    
                    chain.Add(nestedDependency);
                }
            }
        }

        return chain;
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