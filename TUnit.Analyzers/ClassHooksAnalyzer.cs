using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ClassHooksAnalyzer : ConcurrentDiagnosticAnalyzer
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

        var onlyOnceAttributes = attributes
                .Where(x => x.IsNonGlobalHook(context.Compilation) && x.GetHookType() == "TUnit.Core.HookType.Class")
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
            
        if (!IsClassHookContextParameter(methodSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.UnknownParameters,
                context.Symbol.Locations.FirstOrDefault(),
                "empty or only contain `ClassHookContext`")
            );
        }
    }

    private static bool IsClassHookContextParameter(IMethodSymbol methodSymbol)
    {
        if (methodSymbol.Parameters.IsDefaultOrEmpty)
        {
            return true;
        }

        foreach (var parameter in methodSymbol.Parameters)
        {
            if (parameter.Type.GloballyQualified() ==
                WellKnown.AttributeFullyQualifiedClasses.ClassHookContext.WithGlobalPrefix)
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