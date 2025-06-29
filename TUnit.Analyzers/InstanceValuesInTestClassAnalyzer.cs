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
        [Rules.InstanceAssignmentInTestClass];

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

        if (!TryGetParentMethodBody(assignmentOperation, out var methodBodyOperation))
        {
            return;
        }

        if (context.Operation.SemanticModel?.GetDeclaredSymbol(methodBodyOperation.Syntax) is not IMethodSymbol
            methodSymbol)
        {
            return;
        }

        if (!methodSymbol.IsTestMethod(context.Compilation))
        {
            return;
        }

        var testClass = methodSymbol.ContainingType;

        var typeMembers = testClass.GetMembers();

        var fieldsAndProperties = typeMembers
            .OfType<IFieldSymbol>()
            .Concat<ISymbol>(typeMembers.OfType<IPropertySymbol>())
            .Where(x => !x.IsStatic);

        foreach (var fieldOrProperty in fieldsAndProperties)
        {
            var targetSymbol = GetTarget(assignmentOperation);

            if (!SymbolEqualityComparer.Default.Equals(targetSymbol?.ContainingType, testClass))
            {
                return;
            }

            if (SymbolEqualityComparer.Default.Equals(targetSymbol, fieldOrProperty))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.InstanceAssignmentInTestClass,
                        assignmentOperation.Syntax.GetLocation()));
            }
        }
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
