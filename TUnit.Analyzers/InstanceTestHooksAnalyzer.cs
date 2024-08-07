using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Enums;
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

        var onlyOnceAttributes = attributes.Where(x =>
            x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                is WellKnown.AttributeFullyQualifiedClasses.BeforeAttribute
                or WellKnown.AttributeFullyQualifiedClasses.AfterAttribute
                && x.GetHookType() == HookType.EachTest
            )
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
            if (parameter.Type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) ==
                WellKnown.AttributeFullyQualifiedClasses.TestContext)
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