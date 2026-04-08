using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

/// <summary>
/// Reports TUnit0074 when a method marked with <c>[Before(Test)]</c> or <c>[After(Test)]</c>
/// overrides a base method that is also marked with the same hook type. Both registrations are
/// invoked via virtual dispatch, so the override's body runs twice per test.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class VirtualHookOverrideAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.RedundantHookAttributeOnOverride);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol { IsOverride: true } methodSymbol)
        {
            return;
        }

        foreach (var attributeData in methodSymbol.GetAttributes())
        {
            if (!attributeData.IsStandardHook(context.Compilation, out _, out var hookLevel, out var hookType))
            {
                continue;
            }

            // Only instance (Test-level) hooks participate in virtual dispatch; all other hook
            // levels are enforced static elsewhere.
            if (hookLevel != HookLevel.Test)
            {
                continue;
            }

            var conflictingBase = FindBaseWithMatchingHook(methodSymbol, hookType!.Value, context.Compilation);
            if (conflictingBase is null)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rules.RedundantHookAttributeOnOverride,
                attributeData.GetLocation() ?? methodSymbol.Locations.FirstOrDefault(),
                hookType.Value.ToString(),
                conflictingBase.ContainingType?.Name ?? "base",
                conflictingBase.Name));
        }
    }

    // Walks the full override chain (not just the immediate base) — any ancestor with the same
    // hook type causes the duplication. See the Chain_With_Gap test for the transitive case.
    private static IMethodSymbol? FindBaseWithMatchingHook(IMethodSymbol methodSymbol, HookType hookType, Compilation compilation)
    {
        var current = methodSymbol.OverriddenMethod;
        while (current is not null)
        {
            foreach (var baseAttribute in current.GetAttributes())
            {
                if (baseAttribute.IsStandardHook(compilation, out _, out var baseLevel, out var baseType)
                    && baseLevel == HookLevel.Test
                    && baseType == hookType)
                {
                    return current;
                }
            }

            current = current.OverriddenMethod;
        }

        return null;
    }
}
