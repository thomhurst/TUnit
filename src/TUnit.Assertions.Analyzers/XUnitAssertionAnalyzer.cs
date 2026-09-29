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
public class XUnitAssertionAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.XUnitAssertion);

    public override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(compilationStart =>
        {
            // Every reported method lives in a type Xunit.Assert (from any referenced assembly), so skip
            // the per-invocation work entirely when no such type exists.
            if (!HasXunitAssertType(compilationStart.Compilation))
            {
                return;
            }

            compilationStart.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
        });
    }

    // Probes extern-alias-only references too (compilation.GlobalNamespace doesn't merge those in).
    private static bool HasXunitAssertType(Compilation compilation)
        => !TypeSymbolSet.Resolve(compilation, "Xunit.Assert").IsEmpty;

    private static void AnalyzeOperation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        var methodSymbol = invocationOperation.TargetMethod;

        // Cheap pre-filter: "global::Xunit.Assert.*" requires the outermost container to be named Xunit.
        // Avoids building a display string for every invocation in the compilation.
        if (methodSymbol.RendersNameAsLastSegment() && GetOutermostContainerName(methodSymbol) != "Xunit")
        {
            return;
        }

        var fullyQualifiedNonGenericMethodName = methodSymbol.GloballyQualifiedNonGeneric();

        if (fullyQualifiedNonGenericMethodName.StartsWith("global::Xunit.Assert."))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.XUnitAssertion, context.Operation.Syntax.GetLocation())
            );
        }
    }

    private static string GetOutermostContainerName(ISymbol symbol)
    {
        var current = symbol;

        while (current.ContainingSymbol is { } parent && parent is not INamespaceSymbol { IsGlobalNamespace: true })
        {
            current = parent;
        }

        return current.Name;
    }
}
