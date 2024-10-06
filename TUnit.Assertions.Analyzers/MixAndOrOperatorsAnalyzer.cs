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
public class MixAndOrOperatorsAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.MixAndOrConditionsAssertion);

    public override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterOperationAction(AnalyzeOperation, OperationKind.Await);
    }
    
    private static void AnalyzeOperation(OperationAnalysisContext context)
    {
        if (context.Operation is not IAwaitOperation awaitOperation)
        {
            return;
        }
        
        if(awaitOperation.Operation.Type?.AllInterfaces.Any(x => x.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
        is "global::TUnit.Assertions.AssertionBuilders.IInvokableAssertionBuilder") != true)
        {
            return;
        }
    
        var chainedMethodCalls = awaitOperation?.Descendants().OfType<IPropertyReferenceOperation>().ToArray() ?? [];

        if (chainedMethodCalls.Any(x => x.Property.Name == "And")
            && chainedMethodCalls.Any(x => x.Property.Name == "Or"))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MixAndOrConditionsAssertion, awaitOperation?.Syntax.GetLocation()));
        }
    }
}