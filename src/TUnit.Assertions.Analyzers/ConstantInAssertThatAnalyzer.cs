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
public class ConstantInAssertThatAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.ConstantValueInAssertThat);

    public override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(compilationStart =>
        {
            var symbols = AssertionSymbols.For(compilationStart.Compilation);

            if (symbols.Assert.IsEmpty)
            {
                return;
            }

            compilationStart.RegisterOperationAction(ctx => AnalyzeOperation(ctx, symbols), OperationKind.Invocation);
        });
    }

    private static void AnalyzeOperation(OperationAnalysisContext context, AssertionSymbols symbols)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        var targetMethod = invocationOperation.TargetMethod;

        if (!symbols.IsAssertThat(targetMethod))
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
