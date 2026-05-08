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

            var (contextType, contextTypeName) = GetExpectedContext(hookLevel, hookType);

            if (contextType != null)
            {
                var parameterStatus = CheckHookParameters(methodSymbol, contextType);

                switch (parameterStatus)
                {
                    case HookParameterStatus.NoParameters:
                        context.ReportDiagnostic(Diagnostic.Create(
                            Rules.HookContextParameterOptional,
                            methodSymbol.Locations.FirstOrDefault(),
                            contextTypeName));
                        break;

                    case HookParameterStatus.Valid:
                        break;

                    case HookParameterStatus.UnknownParameters:
                        var firstBadParam = FindFirstUnknownParameter(methodSymbol, contextType);
                        context.ReportDiagnostic(Diagnostic.Create(
                            Rules.HookUnknownParameters,
                            firstBadParam?.Locations.FirstOrDefault() ?? methodSymbol.Locations.FirstOrDefault(),
                            contextTypeName));
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

    private static bool IsGlobalHook(SymbolAnalysisContext context, AttributeData x, [NotNullWhen(true)] out HookLevel? hookLevel, [NotNullWhen(true)] out HookType? hookType)
    {
        // For standard hooks (Before/After), only Assembly, TestSession, and TestDiscovery are global
        if (x.IsStandardHook(context.Compilation, out _, out hookLevel, out hookType)
            && hookLevel is HookLevel.Assembly or HookLevel.TestSession or HookLevel.TestDiscovery)
        {
            return true;
        }

        // For Every hooks (BeforeEvery/AfterEvery), all levels (Test, Class, Assembly) are considered global
        return x.IsEveryHook(context.Compilation, out _, out hookLevel, out hookType);
    }

    private static (string? ContextType, string? ContextTypeName) GetExpectedContext(HookLevel? hookLevel, HookType? hookType)
    {
        return hookLevel switch
        {
            HookLevel.Test => (WellKnown.AttributeFullyQualifiedClasses.TestContext.WithGlobalPrefix, "TestContext"),
            HookLevel.Class => (WellKnown.AttributeFullyQualifiedClasses.ClassHookContext.WithGlobalPrefix, "ClassHookContext"),
            HookLevel.Assembly => (WellKnown.AttributeFullyQualifiedClasses.AssemblyHookContext.WithGlobalPrefix, "AssemblyHookContext"),
            HookLevel.TestSession => (WellKnown.AttributeFullyQualifiedClasses.TestSessionContext.WithGlobalPrefix, "TestSessionContext"),
            HookLevel.TestDiscovery when hookType == HookType.Before
                => (WellKnown.AttributeFullyQualifiedClasses.BeforeTestDiscoveryContext.WithGlobalPrefix, "BeforeTestDiscoveryContext"),
            HookLevel.TestDiscovery
                => (WellKnown.AttributeFullyQualifiedClasses.TestDiscoveryContext.WithGlobalPrefix, "TestDiscoveryContext"),
            _ => (null, null)
        };
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
        
        // Single CancellationToken parameter is valid (though context is recommended).
        // Note: A default value is NOT required on the CancellationToken parameter because the
        // framework always provides one. In source-gen mode, the generated hook body delegate always
        // passes cancellationToken. In reflection mode, CreateHookDelegate/CreateInstanceHookDelegate
        // always passes the CancellationToken via method.Invoke args.
        if (parameters.Length == 1 &&
            parameters[0].Type.GloballyQualifiedNonGeneric() == "global::System.Threading.CancellationToken")
        {
            return HookParameterStatus.Valid;
        }

        // Context + CancellationToken is valid (default value not required on CancellationToken;
        // the framework always provides one at invocation time).
        if (parameters.Length == 2 &&
            parameters[0].Type.GloballyQualifiedNonGeneric() == contextType &&
            parameters[1].Type.GloballyQualifiedNonGeneric() == "global::System.Threading.CancellationToken")
        {
            return HookParameterStatus.Valid;
        }
        
        // Anything else is unknown/invalid
        return HookParameterStatus.UnknownParameters;
    }

    private static IParameterSymbol? FindFirstUnknownParameter(IMethodSymbol methodSymbol, string contextType)
    {
        foreach (var parameter in methodSymbol.Parameters)
        {
            var paramType = parameter.Type.GloballyQualifiedNonGeneric();
            if (paramType != contextType &&
                paramType != "global::System.Threading.CancellationToken")
            {
                return parameter;
            }
        }

        return null;
    }
}
