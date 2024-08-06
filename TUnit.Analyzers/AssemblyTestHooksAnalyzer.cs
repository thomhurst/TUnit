using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AssemblyTestHooksAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.MethodMustBeParameterless, Rules.MethodMustNotBeAbstract, Rules.MethodMustBeStatic, Rules.MethodMustBePublic, Rules.UnknownParameters);

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

        var attributes = methodSymbol.GetAttributes();

        var onlyOnceAttributes = attributes.Where(x =>
            x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                is WellKnown.AttributeFullyQualifiedClasses.AssemblySetUp
                or WellKnown.AttributeFullyQualifiedClasses.AssemblyCleanUp
            )
            .ToList();

        if (!onlyOnceAttributes.Any())
        {
            return;
        }

        if (!methodSymbol.IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MethodMustBeStatic,
                context.Symbol.Locations.FirstOrDefault())
            );
        }

        if (methodSymbol.IsAbstract)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MethodMustNotBeAbstract,
                context.Symbol.Locations.FirstOrDefault())
            );
        }
        
        if(methodSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MethodMustBePublic,
                context.Symbol.Locations.FirstOrDefault())
            );
        }
        
        if (!IsAssemblyHookContextParameter(methodSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.UnknownParameters,
                context.Symbol.Locations.FirstOrDefault(),
                "empty or only contain `AssemblyHookContext` and `CancellationToken`")
            );
        }
    }

    private static bool IsAssemblyHookContextParameter(IMethodSymbol methodSymbol)
    {
        if (methodSymbol.Parameters.IsDefaultOrEmpty)
        {
            return true;
        }
        
        foreach (var parameter in methodSymbol.Parameters)
        {
            if (parameter.Type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) ==
                WellKnown.AttributeFullyQualifiedClasses.AssemblyHookContext)
            {
                continue;
            }
            
            if (parameter.Type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) ==
                WellKnown.AttributeFullyQualifiedClasses.CancellationToken)
            {
                continue;
            }

            return false;
        }

        return true;
    }
}