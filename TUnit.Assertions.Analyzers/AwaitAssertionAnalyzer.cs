using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// A sample analyzer that reports the company name being used in class declarations.
/// Traverses through the Syntax Tree and checks the name (identifier) of each class node.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AwaitAssertionAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.AwaitAssertion, Rules.DisposableUsingMultiple);

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

        var methodSymbol = invocationOperation.TargetMethod;

        var fullyQualifiedNonGenericMethodName = methodSymbol.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);
        
        if(fullyQualifiedNonGenericMethodName is "global::TUnit.Assertions.Assert.Multiple")
        {
            CheckMultipleInvocation(context, invocationOperation);
        }
        
        if(fullyQualifiedNonGenericMethodName is "global::TUnit.Assertions.Assert.That")
        {
            CheckAssertInvocation(context, invocationOperation);
        }
    }

    private static void CheckAssertInvocation(OperationAnalysisContext context, IInvocationOperation invocationOperation)
    {
        if (IsAwaited(invocationOperation))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(Rules.AwaitAssertion, context.Operation.Syntax.GetLocation())
        );
    }

    private static void CheckMultipleInvocation(OperationAnalysisContext context, IInvocationOperation invocationOperation)
    {
        if (HasUsing(invocationOperation))
        {
            return;
        }
            
        context.ReportDiagnostic(
            Diagnostic.Create(Rules.DisposableUsingMultiple, context.Operation.Syntax.GetLocation())
        );
    }

    private static bool IsAwaited(IInvocationOperation invocationOperation)
    {
        var parent = invocationOperation.Parent;

        while (parent != null)
        {
            if (parent is IBlockOperation or IDelegateCreationOperation)
            {
                return false;
            }
            
            if (parent is IUsingOperation or IAwaitOperation)
            {
                return true;
            }
            
            parent = parent.Parent;
        }
        
        return false;
    }
    
    private static bool HasUsing(IInvocationOperation invocationOperation)
    {
        var parent = invocationOperation.Parent;

        while (parent != null)
        {
            if (parent is IInvocationOperation or IBlockOperation)
            {
                return false;
            }
            
            if (parent is IUsingOperation or IUsingDeclarationOperation)
            {
                return true;
            }
            
            parent = parent.Parent;
        }
        
        return false;
    }
}