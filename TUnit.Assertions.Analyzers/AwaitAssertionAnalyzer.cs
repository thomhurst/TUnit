using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using TUnit.Assertions.Analyzers.Extensions;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// Reports <c>Assert.That(...)</c> / <c>value.Should()</c> assertion entries that aren't awaited
/// (which silently no-op) and <c>Assert.Multiple()</c> calls without a <c>using</c> declaration.
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

        var fullyQualifiedNonGenericMethodName = methodSymbol.GloballyQualifiedNonGeneric();

        if (fullyQualifiedNonGenericMethodName is "global::TUnit.Assertions.Assert.Multiple")
        {
            CheckMultipleInvocation(context, invocationOperation);
        }

        if (fullyQualifiedNonGenericMethodName is "global::TUnit.Assertions.Assert.That"
                                                 or "global::TUnit.Assertions.Should.ShouldExtensions.Should")
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

    // Walks the syntactic parent chain. Stops at IBlockOperation/IDelegateCreationOperation as
    // negative answers — that means the invocation was used as a statement / lambda body without
    // an enclosing await. This catches the common `value.Should();` mistake but produces a false
    // positive for split-variable patterns:
    //   var src = Assert.That(value);   // ← walk hits the declaration's block before any await
    //   await src.IsEqualTo(...);       //   even though the chain IS awaited here
    // The same applies to `var src = value.Should(); await src.X();`. Both Assert.That and
    // Should() share this limitation by design — fixing requires usage-site dataflow analysis,
    // which is significant complexity for a niche style. Left as a known imprecision so users who
    // hit it know it's not their code.
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
