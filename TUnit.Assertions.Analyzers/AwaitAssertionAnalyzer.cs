using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using TUnit.Assertions.Analyzers.Extensions;

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
        if (fullyQualifiedNonGenericMethodName 
            is not "global::TUnit.Assertions.Assert.That"
            and not "global::TUnit.Assertions.Assert.Multiple")
        {
            return;
        }

        var parentOperations = invocationOperation.GetAncestorOperations().ToArray();
        
        if(fullyQualifiedNonGenericMethodName is "global::TUnit.Assertions.Assert.Multiple"
            && !parentOperations.OfType<IUsingOperation>().Any())
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.DisposableUsingMultiple, context.Operation.Syntax.GetLocation())
            );
            return;
        }

        var operations = parentOperations.SelectMany(x => x.DescendantsAndSelf()).ToArray();
        
        if (operations.OfType<IAwaitOperation>().Any())
        {
            return;
        }
        
        if (operations.OfType<IUsingOperation>().Any())
        {
            return;
        }
        
        context.ReportDiagnostic(
            Diagnostic.Create(Rules.AwaitAssertion, context.Operation.Syntax.GetLocation())
        );
    }
}