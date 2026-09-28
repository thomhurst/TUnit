using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TimeoutCancellationTokenAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Rules.MissingTimeoutCancellationTokenAttributes,
            Rules.CancellationTokenMustBeLastParameter,
            Rules.UseHookCancellationToken);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
        context.RegisterOperationAction(AnalyzeSetupArgument, OperationKind.Argument);
    }

    private static void AnalyzeSetupArgument(OperationAnalysisContext context)
    {
        if (context.Operation is not IArgumentOperation
            {
                Value: IPropertyReferenceOperation
                {
                    Property.Name: "CancellationToken",
                    Instance: IPropertyReferenceOperation
                    {
                        Property.Name: "Execution",
                        Instance: IParameterReferenceOperation parameter
                    } execution
                } token,
                Parent: IInvocationOperation invocation
            } argument
            || !IsDirectSetupOperation(invocation)
            || context.ContainingSymbol is not IMethodSymbol method
            || !IsBeforeTestHook(method, context.Compilation))
        {
            return;
        }

        if (!SymbolEqualityComparer.Default.Equals(argument.Parameter?.Type,
                context.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken"))
            || !SymbolEqualityComparer.Default.Equals(token.Property.ContainingType,
                context.Compilation.GetTypeByMetadataName("TUnit.Core.Interfaces.ITestExecution"))
            || !SymbolEqualityComparer.Default.Equals(execution.Property.ContainingType,
                context.Compilation.GetTypeByMetadataName("TUnit.Core.TestContext"))
            || !SymbolEqualityComparer.Default.Equals(parameter.Parameter.ContainingSymbol, method))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rules.UseHookCancellationToken, token.Syntax.GetLocation()));
    }

    private static bool IsBeforeTestHook(IMethodSymbol method, Compilation compilation)
    {
        for (var current = method; current is not null; current = current.OverriddenMethod)
        {
            foreach (var attribute in current.GetAttributes())
            {
                if ((attribute.IsStandardHook(compilation, out _, out var level, out var type)
                     || attribute.IsEveryHook(compilation, out _, out level, out type))
                    && level == HookLevel.Test
                    && type == HookType.Before)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsDirectSetupOperation(IInvocationOperation invocation)
    {
        IOperation operation = invocation;
        while (operation.Parent is { } parent)
        {
            switch (parent)
            {
                case IConversionOperation { OperatorMethod: null }:
                case IConditionalOperation conditional
                    when conditional.WhenTrue == operation || conditional.WhenFalse == operation:
                case IInvocationOperation { TargetMethod.Name: "ConfigureAwait" } configureAwait
                    when configureAwait.Instance == operation:
                    operation = parent;
                    continue;
            }

            break;
        }

        if (operation.Parent is not (IAwaitOperation or IReturnOperation))
        {
            return false;
        }

        for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is IAnonymousFunctionOperation or ILocalFunctionOperation)
            {
                return false;
            }
        }

        return true;
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (!methodSymbol.IsTestMethod(context.Compilation) &&
            !methodSymbol.IsHookMethod(context.Compilation, out _, out _, out _))
        {
            return;
        }

        var attributes = methodSymbol.GetAttributes()
            .Concat(methodSymbol.ContainingType.GetAttributes());

        var timeoutAttribute = attributes.FirstOrDefault(x => x.AttributeClass?.IsGloballyQualifiedNonGeneric(
                                                             "global::TUnit.Core.TimeoutAttribute") == true);

        if (timeoutAttribute is null)
        {
            return;
        }

        var parameters = methodSymbol.Parameters;

        if (parameters.IsDefaultOrEmpty)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.MissingTimeoutCancellationTokenAttributes,
                    context.Symbol.Locations.FirstOrDefault())
            );
            return;
        }

        var cancellationTokenType = context.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

        var cancellationTokenIndex = -1;
        for (var i = 0; i < parameters.Length; i++)
        {
            if (SymbolEqualityComparer.Default.Equals(parameters[i].Type, cancellationTokenType))
            {
                cancellationTokenIndex = i;
                break;
            }
        }

        var lastParameter = parameters[parameters.Length - 1];

        if (cancellationTokenIndex == -1)
        {
            // CancellationToken is not present at all
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.MissingTimeoutCancellationTokenAttributes,
                    lastParameter.Locations.FirstOrDefault() ?? context.Symbol.Locations.FirstOrDefault())
            );
        }
        else if (cancellationTokenIndex != parameters.Length - 1)
        {
            // CancellationToken exists but is not the last parameter
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.CancellationTokenMustBeLastParameter,
                    parameters[cancellationTokenIndex].Locations.FirstOrDefault() ?? context.Symbol.Locations.FirstOrDefault())
            );
        }
    }
}
