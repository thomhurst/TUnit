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
public class ConstantInAssertThatAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.ConstantValueInAssertThat);

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

        if (targetMethod.Name != "That"
            || !SymbolEqualityComparer.Default.Equals(targetMethod.ContainingType, context.Compilation.GetTypeByMetadataName("TUnit.Assertions.Assert")))
        {
            return;
        }

        // True if constant
        var firstArgument = invocationOperation.Arguments[0];
        if (firstArgument.ConstantValue.HasValue || firstArgument.Value.ConstantValue.HasValue)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.ConstantValueInAssertThat, invocationOperation.Syntax.GetLocation())
            );
        }
    }
}
