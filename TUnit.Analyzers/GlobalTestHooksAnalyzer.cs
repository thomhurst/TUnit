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
            Rules.HookContextParameterOptional,
            Rules.HookUnknownParameters
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

            var contextType = hookLevel switch
            {
                HookLevel.Test => WellKnown.AttributeFullyQualifiedClasses.TestContext.WithGlobalPrefix,
                HookLevel.Class => WellKnown.AttributeFullyQualifiedClasses.ClassHookContext.WithGlobalPrefix,
                HookLevel.Assembly => WellKnown.AttributeFullyQualifiedClasses.AssemblyHookContext.WithGlobalPrefix,
                _ => null
            };

            if (contextType != null)
            {
                var parameterStatus = CheckHookParameters(methodSymbol, contextType);
                
                switch (parameterStatus)
                {
                    case HookParameterStatus.NoParameters:
                        // Informational diagnostic - suggest adding context parameter
                        var contextTypeName = hookLevel switch
                        {
                            HookLevel.Test => "TestContext",
                            HookLevel.Class => "ClassHookContext",
                            HookLevel.Assembly => "AssemblyHookContext",
                            _ => "context"
                        };
                        context.ReportDiagnostic(Diagnostic.Create(
                            Rules.HookContextParameterOptional, 
                            methodSymbol.Locations.FirstOrDefault(),
                            contextTypeName));
                        break;
                        
                    case HookParameterStatus.Valid:
                        // No diagnostic needed - parameters are correct
                        break;
                        
                    case HookParameterStatus.UnknownParameters:
                        // Error diagnostic - unknown parameters
                        var expectedContextTypeName = hookLevel switch
                        {
                            HookLevel.Test => "TestContext",
                            HookLevel.Class => "ClassHookContext",
                            HookLevel.Assembly => "AssemblyHookContext",
                            _ => "context"
                        };
                        context.ReportDiagnostic(Diagnostic.Create(
                            Rules.HookUnknownParameters, 
                            methodSymbol.Locations.FirstOrDefault(),
                            expectedContextTypeName));
                        break;
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

    private enum HookParameterStatus
    {
        NoParameters,
        Valid,
        UnknownParameters
    }

    private static HookParameterStatus CheckHookParameters(IMethodSymbol methodSymbol, string contextType)
    {
        var parameters = methodSymbol.Parameters;
        
        // No parameters - valid but we'll suggest adding context
        if (parameters.Length == 0)
        {
            return HookParameterStatus.NoParameters;
        }
        
        // Single context parameter is valid
        if (parameters.Length == 1 && 
            parameters[0].Type.GloballyQualifiedNonGeneric() == contextType)
        {
            return HookParameterStatus.Valid;
        }
        
        // Single CancellationToken parameter is valid (though context is recommended)
        if (parameters.Length == 1 && 
            parameters[0].Type.GloballyQualifiedNonGeneric() == "global::System.Threading.CancellationToken")
        {
            return HookParameterStatus.Valid;
        }
        
        // Context + CancellationToken is valid
        if (parameters.Length == 2 && 
            parameters[0].Type.GloballyQualifiedNonGeneric() == contextType &&
            parameters[1].Type.GloballyQualifiedNonGeneric() == "global::System.Threading.CancellationToken")
        {
            return HookParameterStatus.Valid;
        }
        
        // Anything else is unknown/invalid
        return HookParameterStatus.UnknownParameters;
    }
}
