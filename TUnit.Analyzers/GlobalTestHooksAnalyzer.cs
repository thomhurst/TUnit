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
                && !HasSingleParameter(methodSymbol, WellKnown.AttributeFullyQualifiedClasses.AssemblyHookContext.WithGlobalPrefix)
                && attributeData.IsEveryHook(context.Compilation, out _, out _, out _))
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
        if (x.IsStandardHook(context.Compilation, out _, out hookLevel, out _)
            && hookLevel is HookLevel.Assembly or HookLevel.TestSession or HookLevel.TestDiscovery)
        {
            return true;
        }

        return x.IsEveryHook(context.Compilation, out _, out hookLevel, out _);
    }

    private static bool HasSingleParameter(IMethodSymbol methodSymbol, string parameterType)
    {
        return methodSymbol.Parameters.WithoutCancellationTokenParameter().Count() == 1
               && methodSymbol.Parameters[0].Type.GloballyQualifiedNonGeneric() == parameterType;
    }
}
