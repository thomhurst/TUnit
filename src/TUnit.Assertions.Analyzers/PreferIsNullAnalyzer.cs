using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// Detects usage of .IsEqualTo(null) and suggests using .IsNull() instead
/// for clearer intent and better error messages.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PreferIsNullAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.PreferIsNullOverIsEqualToNull);

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

        // Check that this is a TUnit assertion method by checking the return type
        // inherits from or is in the TUnit.Assertions namespace
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

        if (IsNullConstant(expectedArgument.Value))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.PreferIsNullOverIsEqualToNull, invocationOperation.Syntax.GetLocation())
            );
        }
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

    private static bool IsNullConstant(IOperation operation)
    {
        // Direct null literal
        if (operation is ILiteralOperation { ConstantValue: { HasValue: true, Value: null } })
        {
            return true;
        }

        // Constant value is null
        if (operation.ConstantValue is { HasValue: true, Value: null })
        {
            return true;
        }

        // Check through conversion (null is often implicitly converted)
        if (operation is IConversionOperation conversionOperation)
        {
            return IsNullConstant(conversionOperation.Operand);
        }

        // Check for default literal with reference types (default is null for reference types)
        if (operation is IDefaultValueOperation defaultOp && defaultOp.Type?.IsReferenceType == true)
        {
            return true;
        }

        return false;
    }
}
