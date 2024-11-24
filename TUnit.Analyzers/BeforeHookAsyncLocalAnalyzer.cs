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
        ImmutableArray.Create(Rules.AsyncLocalVoidMethod);

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

        var parent = assignmentOperation.Parent;
        while (parent != null)
        {
            if (parent is IMethodBodyOperation methodBodyOperation)
            {
                CheckMethod(context, methodBodyOperation);
            }

            parent = parent.Parent;
        }
    }

    private void CheckMethod(OperationAnalysisContext context, IMethodBodyOperation methodBodyOperation)
    {
        if (methodBodyOperation.SemanticModel?.GetDeclaredSymbol(methodBodyOperation.Syntax) is not IMethodSymbol
            methodSymbol)
        {
            return;
        }

        if (methodSymbol.IsHookMethod(context.Compilation, out _, out _, out var type)
            && type is HookType.Before 
            && !methodSymbol.ReturnsVoid)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.AsyncLocalVoidMethod,
                context.Operation.Syntax.GetLocation(),
                [methodBodyOperation.Syntax.GetLocation()]));
            return;
        }

        var invocations = methodBodyOperation.SemanticModel
            .SyntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>();
        
        foreach (var invocationExpressionSyntax in invocations)
        {
            var operation = methodBodyOperation.SemanticModel.GetOperation(invocationExpressionSyntax);

            if (operation is not IInvocationOperation invocationOperation)
            {
                continue;
            }
            
            if (!SymbolEqualityComparer.Default.Equals(invocationOperation.TargetMethod, methodSymbol))
            {
                continue;
            }

            var parentMethodBody = GetParentMethodBody(invocationOperation);

            if (parentMethodBody == null)
            {
                continue;
            }
            
            CheckMethod(context, parentMethodBody);
        }
    }

    private IMethodBodyOperation? GetParentMethodBody(IInvocationOperation invocationOperation)
    {
        var parent = invocationOperation.Parent;

        while (parent != null)
        {
            if (parent is IMethodBodyOperation methodBodyOperation)
            {
                return methodBodyOperation;
            }
            
            parent = parent.Parent;
        }

        return null;
    }
}