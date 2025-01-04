using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
        Rules.SingleAssemblyHookContextParameterRequired,
        Rules.SingleTestSessionHookContextParameterRequired,
        Rules.SingleTestDiscoveryHookContextParameterRequired,
        Rules.SingleBeforeTestDiscoveryHookContextParameterRequired
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
            .Where(x => IsGlobalHook(context, x, out _, out _))
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
        
        if (methodSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MethodMustBePublic,
                context.Symbol.Locations.FirstOrDefault())
            );
        }
        
        foreach (var attributeData in globalHooks)
        {
            IsGlobalHook(context, attributeData, out var hookLevel, out var hookType);
            
            if (hookLevel == HookLevel.Test
                && !HasSingleParameter(methodSymbol, WellKnown.AttributeFullyQualifiedClasses.TestContext.WithGlobalPrefix))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.SingleTestContextParameterRequired, methodSymbol.Locations.FirstOrDefault()));
            }
            
            else if (hookLevel == HookLevel.Class
                && !HasSingleParameter(methodSymbol, WellKnown.AttributeFullyQualifiedClasses.ClassHookContext.WithGlobalPrefix))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.SingleClassHookContextParameterRequired, methodSymbol.Locations.FirstOrDefault()));
            }
            
            else if (hookLevel == HookLevel.Assembly
                && !HasSingleParameter(methodSymbol, WellKnown.AttributeFullyQualifiedClasses.AssemblyHookContext.WithGlobalPrefix))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.SingleAssemblyHookContextParameterRequired, methodSymbol.Locations.FirstOrDefault()));
            }
            
            else if (hookLevel == HookLevel.TestSession
                && !HasSingleParameter(methodSymbol, WellKnown.AttributeFullyQualifiedClasses.TestSessionContext.WithGlobalPrefix))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.SingleTestSessionHookContextParameterRequired, methodSymbol.Locations.FirstOrDefault()));
            }
            
            else if (hookLevel == HookLevel.TestDiscovery)
            {
                if (hookType == HookType.Before && !HasSingleParameter(methodSymbol, WellKnown.AttributeFullyQualifiedClasses.BeforeTestDiscoveryContext.WithGlobalPrefix))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rules.SingleBeforeTestDiscoveryHookContextParameterRequired, methodSymbol.Locations.FirstOrDefault()));
                }
                else if (hookType == HookType.After && !HasSingleParameter(methodSymbol, WellKnown.AttributeFullyQualifiedClasses.TestDiscoveryContext.WithGlobalPrefix))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rules.SingleTestDiscoveryHookContextParameterRequired, methodSymbol.Locations.FirstOrDefault()));
                }
            }
        }
        
        if (methodSymbol.ContainingType.IsTestClass(context.Compilation))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.GlobalHooksSeparateClass,
                context.Symbol.Locations.FirstOrDefault())
            );
        }
    }

    private static bool IsGlobalHook(SymbolAnalysisContext context, AttributeData x, [NotNullWhen(true)] out HookLevel? hookLevel, [NotNullWhen(true)] out HookType? hookType)
    {
        if (x.IsStandardHook(context.Compilation, out _, out hookLevel, out hookType)
            && hookLevel is HookLevel.Assembly or HookLevel.TestSession or HookLevel.TestDiscovery)
        {
            return true;
        }
        
        return x.IsEveryHook(context.Compilation, out _, out hookLevel, out hookType);
    }

    private static bool HasSingleParameter(IMethodSymbol methodSymbol, string parameterType)
    {
        return methodSymbol.Parameters.WithoutCancellationTokenParameter().Count() == 1
               && methodSymbol.Parameters[0].Type.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) == parameterType;
    }
}