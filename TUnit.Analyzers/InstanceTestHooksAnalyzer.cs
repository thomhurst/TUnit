using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InstanceTestHooksAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.MethodMustNotBeStatic, Rules.MethodMustBePublic, Rules.MethodMustBeParameterless);

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

        var onlyOnceAttributes = attributes
            .Where(x => x.IsNonGlobalHook(context.Compilation) && x.GetHookType() == "TUnit.Core.HookType.Test")
            .ToList();

        if (!onlyOnceAttributes.Any())
        {
            return;
        }

        if (methodSymbol.IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MethodMustNotBeStatic,
                context.Symbol.Locations.FirstOrDefault())
            );
        }

        if (!IsContextParameter(methodSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MethodMustBeParameterless,
                context.Symbol.Locations.FirstOrDefault())
            );
        }
        
        if(methodSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MethodMustBePublic,
                context.Symbol.Locations.FirstOrDefault())
            );
        }
    }
    
    private static bool IsContextParameter(IMethodSymbol methodSymbol)
    {
        if (methodSymbol.Parameters.IsDefaultOrEmpty)
        {
            return true;
        }

        foreach (var parameter in methodSymbol.Parameters)
        {
            if (parameter.Type.GloballyQualified() ==
                WellKnown.AttributeFullyQualifiedClasses.TestContext.WithGlobalPrefix)
            {
                continue;
            }

            if (parameter.Type.GloballyQualified() ==
                WellKnown.AttributeFullyQualifiedClasses.CancellationToken.WithGlobalPrefix)
            {
                continue;
            }

            return false;
        }

        return true;
    }
}