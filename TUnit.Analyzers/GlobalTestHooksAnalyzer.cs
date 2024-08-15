using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GlobalTestHooksAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    ImmutableArray.Create
    (
        Rules.MethodMustNotBeAbstract,
        Rules.MethodMustBeStatic,
        Rules.MethodMustBePublic,
        Rules.GlobalHooksSeparateClass,
        Rules.SingleTestContextParameterRequired,
        Rules.SingleClassHookContextParameterRequired,
        Rules.SingleAssemblyHookContextParameterRequired
    );

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

        var globalHooks = attributes
            .Where(x => x.IsGlobalHook())
            .ToList();

        if (!globalHooks.Any())
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
        
        foreach (var attributeData in globalHooks)
        {
            if (attributeData.GetHookType() == Core.HookType.EachTest
                && !HasSingleParameter(methodSymbol, WellKnown.AttributeFullyQualifiedClasses.TestContext))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.SingleTestContextParameterRequired, methodSymbol.Locations.FirstOrDefault()));
            }
            
            if (attributeData.GetHookType() == Core.HookType.Class
                && !HasSingleParameter(methodSymbol, WellKnown.AttributeFullyQualifiedClasses.ClassHookContext))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.SingleTestContextParameterRequired, methodSymbol.Locations.FirstOrDefault()));
            }
            
            if (attributeData.GetHookType() == Core.HookType.Assembly
                && !HasSingleParameter(methodSymbol, WellKnown.AttributeFullyQualifiedClasses.AssemblyHookContext))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.SingleTestContextParameterRequired, methodSymbol.Locations.FirstOrDefault()));
            }
        }
        
        if (methodSymbol.ContainingType.IsTestClass())
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.GlobalHooksSeparateClass,
                context.Symbol.Locations.FirstOrDefault())
            );
        }
    }

    private static bool HasSingleParameter(IMethodSymbol methodSymbol, string parameterType)
    {
        return methodSymbol.Parameters.WithoutTimeoutParameter().Count() == 1
               && methodSymbol.Parameters[0].Type.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) == parameterType;
    }
}