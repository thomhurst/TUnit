using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// Detects usage of .IsEqualTo(true) or .IsEqualTo(false) and suggests using
/// .IsTrue() or .IsFalse() instead for clearer intent and better error messages.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PreferIsTrueOrIsFalseAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.PreferIsTrueOrIsFalseOverIsEqualToBool);

    public override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
    }

    private void AnalyzeOperation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        var targetMethod = invocationOperation.TargetMethod;

        if (targetMethod.Name != "IsEqualTo")
        {
            return;
        }

        // Check that this is a TUnit assertion method
        if (!IsTUnitAssertionMethod(targetMethod))
        {
            return;
        }

        // Find the 'expected' parameter - for extension methods, the first parameter
        // is the 'this' parameter, so we need to find the right one by name
        var expectedArgument = invocationOperation.Arguments
            .FirstOrDefault(a => a.Parameter?.Name == "expected");

        if (expectedArgument is null)
        {
            return;
        }

        var boolValue = GetBooleanConstantValue(expectedArgument.Value);

        if (boolValue is null)
        {
            return;
        }

        var suggestedMethod = boolValue.Value ? "IsTrue" : "IsFalse";
        var literalText = boolValue.Value ? "true" : "false";

        context.ReportDiagnostic(
            Diagnostic.Create(
                Rules.PreferIsTrueOrIsFalseOverIsEqualToBool,
                invocationOperation.Syntax.GetLocation(),
                suggestedMethod,
                literalText)
        );
    }

    private static bool IsTUnitAssertionMethod(IMethodSymbol method)
    {
        // Check if the return type is in the TUnit.Assertions namespace
        var returnTypeNamespace = method.ReturnType?.ContainingNamespace?.ToDisplayString();
        if (returnTypeNamespace != null && returnTypeNamespace.StartsWith("TUnit.Assertions"))
        {
            return true;
        }

        // Check if the containing type is in the TUnit.Assertions namespace
        var containingNamespace = method.ContainingType?.ContainingNamespace?.ToDisplayString();
        if (containingNamespace != null && containingNamespace.StartsWith("TUnit.Assertions"))
        {
            return true;
        }

        return false;
    }

    private static bool? GetBooleanConstantValue(IOperation operation)
    {
        // Direct literal check
        if (operation is ILiteralOperation { ConstantValue: { HasValue: true, Value: bool directValue } })
        {
            return directValue;
        }

        // Check through constant value
        if (operation.ConstantValue is { HasValue: true, Value: bool constantValue })
        {
            return constantValue;
        }

        // Check through conversion (e.g., implicit conversions)
        if (operation is IConversionOperation conversionOperation)
        {
            return GetBooleanConstantValue(conversionOperation.Operand);
        }

        return null;
    }
}
