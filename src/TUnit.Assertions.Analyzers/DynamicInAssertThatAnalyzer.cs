using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using TUnit.Assertions.Analyzers.Helpers;

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
        context.RegisterCompilationStartAction(compilationStart =>
        {
            var symbols = AssertionSymbols.For(compilationStart.Compilation);

            if (symbols.Assert.IsEmpty)
            {
                return;
            }

            compilationStart.RegisterOperationAction(ctx => AnalyzeOperation(ctx, symbols), OperationKind.DynamicInvocation);
        });
    }

    private static void AnalyzeOperation(OperationAnalysisContext context, AssertionSymbols symbols)
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

        if (!symbols.IsAssertThat(targetMethod))
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
