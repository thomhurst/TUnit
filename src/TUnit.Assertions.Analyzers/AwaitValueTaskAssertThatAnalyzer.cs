using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using TUnit.Assertions.Analyzers.Extensions;
using TUnit.Assertions.Analyzers.Helpers;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// A sample analyzer that reports the company name being used in class declarations.
/// Traverses through the Syntax Tree and checks the name (identifier) of each class node.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AwaitValueTaskAssertThatAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.AwaitValueTaskInAssertThat);

    public override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(compilationStart =>
        {
            var symbols = AssertionSymbols.For(compilationStart.Compilation);
            var valueTask = compilationStart.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
            var genericValueTask = compilationStart.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

            if (symbols.Assert.IsEmpty || valueTask is null || genericValueTask is null)
            {
                return;
            }

            compilationStart.RegisterOperationAction(
                ctx => AnalyzeOperation(ctx, symbols, valueTask, genericValueTask),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzeOperation(
        OperationAnalysisContext context,
        AssertionSymbols symbols,
        INamedTypeSymbol valueTask,
        INamedTypeSymbol genericValueTask)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        var methodSymbol = invocationOperation.TargetMethod;

        if (!symbols.IsAssertThat(methodSymbol))
        {
            return;
        }

        var funcArgumentOperation = invocationOperation.Arguments.First();

        var type = funcArgumentOperation.Parameter?.Type;

        if (type?.IsOrInherits(valueTask) is true || type?.OriginalDefinition?.IsOrInherits(genericValueTask) is true)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.AwaitValueTaskInAssertThat, context.Operation.Syntax.GetLocation())
            );

            return;
        }

        if (type is not INamedTypeSymbol namedTypeSymbol)
        {
            return;
        }

        if (namedTypeSymbol.DelegateInvokeMethod?.ReturnType.IsOrInherits(valueTask) is not true
           && namedTypeSymbol.DelegateInvokeMethod?.ReturnType.OriginalDefinition.IsOrInherits(genericValueTask) is not true)
        {
            return;
        }

        if (funcArgumentOperation.Descendants().Any(x => x.Kind == OperationKind.Await))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(Rules.AwaitValueTaskInAssertThat, context.Operation.Syntax.GetLocation())
        );
    }
}
