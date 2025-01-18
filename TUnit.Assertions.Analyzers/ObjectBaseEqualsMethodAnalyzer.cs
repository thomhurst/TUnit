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
public class ObjectBaseEqualsMethodAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.ObjectEqualsBaseMethod);

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

        if (invocationOperation.TargetMethod.Name != "Equals")
        {
            return;
        }
        
        if ((invocationOperation.Instance?.Type as INamedTypeSymbol)
            ?.AllInterfaces
            .Select(x => x.GloballyQualifiedNonGeneric())
            .Any(x => x is "global::TUnit.Assertions.AssertConditions.Interfaces.IValueSource"
            or "global::TUnit.Assertions.AssertConditions.Interfaces.IDelegateSource") != true)
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(Rules.ObjectEqualsBaseMethod, invocationOperation.Syntax.GetLocation())
        );
    }
}