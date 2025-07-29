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
        ImmutableArray.Create(
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
            .Where(x => IsGlobalHook(context, x, out _))
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
            IsGlobalHook(context, attributeData, out var hookLevel);

            if (hookLevel == HookLevel.Test
                && !HasValidHookParameters(methodSymbol, WellKnown.AttributeFullyQualifiedClasses.TestContext.WithGlobalPrefix))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.SingleTestContextParameterRequired, methodSymbol.Locations.FirstOrDefault()));
            }

            else if (hookLevel == HookLevel.Class
                && !HasValidHookParameters(methodSymbol, WellKnown.AttributeFullyQualifiedClasses.ClassHookContext.WithGlobalPrefix))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.SingleClassHookContextParameterRequired, methodSymbol.Locations.FirstOrDefault()));
            }

            else if (hookLevel == HookLevel.Assembly
                && !HasValidHookParameters(methodSymbol, WellKnown.AttributeFullyQualifiedClasses.AssemblyHookContext.WithGlobalPrefix))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.SingleAssemblyHookContextParameterRequired, methodSymbol.Locations.FirstOrDefault()));
            }
        }

        if (methodSymbol.ContainingType.IsTestClass(context.Compilation))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.GlobalHooksSeparateClass,
                context.Symbol.Locations.FirstOrDefault())
            );
        }
    }

    private static bool IsGlobalHook(SymbolAnalysisContext context, AttributeData x, [NotNullWhen(true)] out HookLevel? hookLevel)
    {
        // For standard hooks (Before/After), only Assembly, TestSession, and TestDiscovery are global
        if (x.IsStandardHook(context.Compilation, out _, out hookLevel, out _)
            && hookLevel is HookLevel.Assembly or HookLevel.TestSession or HookLevel.TestDiscovery)
        {
            return true;
        }

        // For Every hooks (BeforeEvery/AfterEvery), all levels (Test, Class, Assembly) are considered global
        return x.IsEveryHook(context.Compilation, out _, out hookLevel, out _);
    }

    private static bool HasValidHookParameters(IMethodSymbol methodSymbol, string contextType)
    {
        var parameters = methodSymbol.Parameters;
        
        // For Test, Class, and Assembly level hooks, a context parameter is REQUIRED
        // Valid options are:
        // 1. Single context parameter
        // 2. Context parameter + CancellationToken
        
        // Single context parameter is valid
        if (parameters.Length == 1 && 
            parameters[0].Type.GloballyQualifiedNonGeneric() == contextType)
        {
            return true;
        }
        
        // Context + CancellationToken is valid
        if (parameters.Length == 2 && 
            parameters[0].Type.GloballyQualifiedNonGeneric() == contextType &&
            parameters[1].Type.GloballyQualifiedNonGeneric() == "global::System.Threading.CancellationToken")
        {
            return true;
        }
        
        return false;
    }
}
