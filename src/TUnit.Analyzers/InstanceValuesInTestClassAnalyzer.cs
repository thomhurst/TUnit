using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InstanceValuesInTestClassAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.InstanceAssignmentInTestClass);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterOperationAction(AnalyzeOperation, OperationKind.SimpleAssignment);
    }

    private void AnalyzeOperation(OperationAnalysisContext context)
    {
        if (context.Operation is not IAssignmentOperation assignmentOperation)
        {
            return;
        }

        // Cheapest checks first: only assignments to instance fields/properties can be reported.
        var targetSymbol = GetTarget(assignmentOperation);

        if (targetSymbol is null || targetSymbol.IsStatic)
        {
            return;
        }

        // Only assignments inside a method body (including lambdas and local functions within it).
        if (!TryGetParentMethodBody(assignmentOperation, out _))
        {
            return;
        }

        // The operation block's owner is the method whose body we're in; no need to re-bind its declaration.
        if (context.ContainingSymbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        var testClass = methodSymbol.ContainingType;

        // The target must be a member of the test class itself.
        if (!SymbolEqualityComparer.Default.Equals(targetSymbol.ContainingType, testClass))
        {
            return;
        }

        if (!methodSymbol.IsTestMethod(context.Compilation))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(Rules.InstanceAssignmentInTestClass,
                assignmentOperation.Syntax.GetLocation()));
    }

    private static ISymbol? GetTarget(IAssignmentOperation assignmentOperation)
    {
        if (assignmentOperation.Target is IPropertyReferenceOperation propertyReferenceOperation)
        {
            return propertyReferenceOperation.Property;
        }

        if (assignmentOperation.Target is IFieldReferenceOperation fieldReferenceOperation)
        {
            return fieldReferenceOperation.Field;
        }

        return null;
    }

    private static bool TryGetParentMethodBody(IAssignmentOperation assignmentOperation, [NotNullWhen(true)] out IMethodBodyOperation? methodBodyOperation)
    {
        var parent = assignmentOperation.Parent;

        while (parent is not null)
        {
            if (parent is IMethodBodyOperation methodBody)
            {
                methodBodyOperation = methodBody;
                return true;
            }

            parent = parent.Parent;
        }

        methodBodyOperation = null;
        return false;
    }
}
