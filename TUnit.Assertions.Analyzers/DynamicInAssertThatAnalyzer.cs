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
public class DynamicInAssertThatAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.DynamicValueInAssertThat);

    public override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterOperationAction(AnalyzeOperation, OperationKind.DynamicInvocation);
    }

    private void AnalyzeOperation(OperationAnalysisContext context)
    {
        if (context.Operation is not IDynamicInvocationOperation dynamicInvocationOperation)
        {
            return;
        }

        if (context.Operation.SemanticModel?.GetSymbolInfo(context.Operation.Syntax).CandidateSymbols.FirstOrDefault() is
            not IMethodSymbol targetMethod)
        {
            return;
        }

        if (targetMethod.Name != "That"
            || !SymbolEqualityComparer.Default.Equals(targetMethod.ContainingType, context.Compilation.GetTypeByMetadataName("TUnit.Assertions.Assert")))
        {
            return;
        }

        var firstArgument = dynamicInvocationOperation.Arguments[0];

        if (SymbolEqualityComparer.Default.Equals(firstArgument.Type, context.Compilation.DynamicType))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.DynamicValueInAssertThat, dynamicInvocationOperation.Syntax.GetLocation())
            );
        }
    }
}
