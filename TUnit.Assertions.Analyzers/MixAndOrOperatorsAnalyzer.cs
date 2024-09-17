using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using TUnit.Assertions.Analyzers.Extensions;

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
        context.RegisterOperationAction(AnalyzeOperation, OperationKind.PropertyReference);
    }
    
    private static void AnalyzeOperation(OperationAnalysisContext context)
    {
        if (context.Operation is not IPropertyReferenceOperation propertyReferenceOperation)
        {
            return;
        }
        
        if (propertyReferenceOperation.Property.Name is not "And" and not "Or")
        {
            return;
        }
        
        if(propertyReferenceOperation.Property.ContainingType.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) !=
            "global::TUnit.Assertions.AssertionBuilders.InvokableAssertionBuilder")
        {
            return;
        }

        var awaitOperation = propertyReferenceOperation.GetAncestorOperations().OfType<IAwaitOperation>().FirstOrDefault();
        var chainedMethodCalls = awaitOperation?.Descendants().OfType<IPropertyReferenceOperation>().ToArray() ?? [];

        if (chainedMethodCalls.Any(x => x.Property.Name == "And")
            && chainedMethodCalls.Any(x => x.Property.Name == "Or"))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MixAndOrConditionsAssertion,
                awaitOperation?.Syntax.GetLocation() ?? propertyReferenceOperation.Syntax.GetLocation())
            );
        }
    }
}