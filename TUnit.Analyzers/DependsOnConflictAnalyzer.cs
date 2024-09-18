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
        if (context.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        var dependencies = GetDependencies(context, methodSymbol, methodSymbol).ToArray();

        if (!dependencies.Any() || !dependencies.Contains(methodSymbol, SymbolEqualityComparer.Default))
        {
            return;
        }
        
        context.ReportDiagnostic(Diagnostic.Create(Rules.DependsOnConflicts, methodSymbol.Locations.FirstOrDefault(), string.Join(" > ", [methodSymbol.Name, ..dependencies.Select(x => x.Name)])));
    }

    private IEnumerable<IMethodSymbol> GetDependencies(SymbolAnalysisContext context, IMethodSymbol originalMethod, IMethodSymbol methodToGetDependenciesFor)
    {
        if (!methodToGetDependenciesFor.IsTestMethod())
        {
            yield break;
        }
        
        var attributes = methodToGetDependenciesFor.GetAttributes();

        var dependsOnAttributes = attributes.Where(x =>
                x.AttributeClass?.IsOrInherits(WellKnown.AttributeFullyQualifiedClasses.DependsOnAttribute) == true)
            .ToArray();

        if (!dependsOnAttributes.Any())
        {
            yield break;
        }

        var dependencyNames = dependsOnAttributes.Select(x => x.ConstructorArguments.First().Value).OfType<string>().ToArray();
        var parameterTypesArray = dependsOnAttributes.Select(x =>
        {
            var parameterArgs = x.ConstructorArguments.ElementAtOrDefault(1);
            return parameterArgs.Kind == TypedConstantKind.Array ? parameterArgs.Values.Select(tc => (INamedTypeSymbol)tc.Value!).ToArray() : [];
        }).ToArray();

        var methods = methodToGetDependenciesFor.ReceiverType?.GetMembers().OfType<IMethodSymbol>().ToArray() ?? [];

        if (!methods.Any())
        {
            foreach (var dependsOnAttribute in dependsOnAttributes)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.NoMethodFound, dependsOnAttribute.GetLocation()));
            }

            yield break;
        }
        
        for (var i = 0; i < dependencyNames.Length; i++)
        {
            var name = dependencyNames.ElementAtOrDefault(i);
            var parameterTypes = parameterTypesArray.ElementAtOrDefault(i) ?? [];

            var foundDependency = methods.SingleOrDefault(x => x.Name == name)
                                  ?? methods.FirstOrDefault(x => x.Name == name && x.Parameters.Select(p => p.Type).SequenceEqual(parameterTypes, SymbolEqualityComparer.Default));

            if (foundDependency == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.NoMethodFound, dependsOnAttributes[i].GetLocation()));
                continue;
            }

            yield return foundDependency;
            
            if (SymbolEqualityComparer.Default.Equals(originalMethod, foundDependency))
            {
                yield break;
            }
            
            foreach (var nestedDependency in GetDependencies(context, originalMethod, foundDependency))
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