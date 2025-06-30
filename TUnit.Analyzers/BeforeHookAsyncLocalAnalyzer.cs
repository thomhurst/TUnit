using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BeforeHookAsyncLocalAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        new()
        {
            Rules.AsyncLocalCallFlowValues
        };

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

        if (assignmentOperation.Target is not IPropertyReferenceOperation propertyReferenceOperation)
        {
            return;
        }

        var propertyContainingType = propertyReferenceOperation.Property.ContainingType;

        if (!propertyContainingType.IsGenericType
            || !SymbolEqualityComparer.Default.Equals(propertyContainingType.OriginalDefinition,
                context.Compilation.GetTypeByMetadataName(typeof(AsyncLocal<object>).GetMetadataName())))
        {
            return;
        }

        var methodBodyOperation = GetParentMethod(assignmentOperation.Parent);

        if (methodBodyOperation is null)
        {
            return;
        }

        CheckMethod(context, methodBodyOperation);
    }

    private IMethodBodyOperation? GetParentMethod(IOperation? assignmentOperationParent)
    {
        var parent = assignmentOperationParent;

        while (parent is not null)
        {
            if (parent is IMethodBodyOperation methodBodyOperation)
            {
                return methodBodyOperation;
            }

            parent = parent.Parent;
        }

        return null;
    }

    private void CheckMethod(OperationAnalysisContext context, IMethodBodyOperation methodBodyOperation)
    {
        if (methodBodyOperation.SemanticModel?.GetDeclaredSymbol(methodBodyOperation.Syntax) is not IMethodSymbol
            methodSymbol)
        {
            return;
        }

        if (!methodSymbol.IsHookMethod(context.Compilation, out _, out _, out var type)
            || type is not HookType.Before)
        {
            return;
        }

        var syntax = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();

        if (syntax is null)
        {
            return;
        }

        var invocations = syntax
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>();

        foreach (var invocationExpressionSyntax in invocations)
        {
            var operation = methodBodyOperation.SemanticModel.GetOperation(invocationExpressionSyntax);

            if (operation is not IInvocationOperation invocationOperation)
            {
                continue;
            }

            if (invocationOperation.TargetMethod.Name == "AddAsyncLocalValues")
            {
                return;
            }
        }

        context.ReportDiagnostic(Diagnostic.Create(Rules.AsyncLocalCallFlowValues,
            methodBodyOperation.Syntax.GetLocation()));
    }
}
