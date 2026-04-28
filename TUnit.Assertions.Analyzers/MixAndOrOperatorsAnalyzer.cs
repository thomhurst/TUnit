using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using TUnit.Assertions.Analyzers.Extensions;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// Reports awaited assertion chains that mix <c>.And</c> and <c>.Or</c> combinators without
/// explicit grouping — the runtime throws <c>MixedAndOrAssertionsException</c>, this surfaces it
/// at compile time. Covers both <c>Assert.That</c> and <c>value.Should()</c> entry points.
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

        // Check if the awaited type implements IAssertionSource<T>/IShouldSource<T> or
        // inherits from Assertion<T>/ShouldAssertion<T>.
        var awaitedType = awaitOperation.Operation.Type;
        var isAssertionSource = awaitedType?.AllInterfaces.Any(x =>
            x.GloballyQualifiedNonGeneric() is "global::TUnit.Assertions.Core.IAssertionSource"
                                            or "global::TUnit.Assertions.Should.Core.IShouldSource") == true;
        var isAssertion = (awaitedType?.BaseType != null && IsAssertionType(awaitedType.BaseType))
                       || IsShouldAssertionType(awaitedType);

        if (!isAssertionSource && !isAssertion)
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

    private static bool IsAssertionType(INamedTypeSymbol? type)
    {
        if (type == null)
        {
            return false;
        }

        // Check if this type is Assertion<T>
        if (type.GloballyQualifiedNonGeneric() is "global::TUnit.Assertions.Core.Assertion")
        {
            return true;
        }

        // Check base type recursively
        return IsAssertionType(type.BaseType);
    }

    private static bool IsShouldAssertionType(ITypeSymbol? type)
        => type is INamedTypeSymbol named
           && named.GloballyQualifiedNonGeneric() is "global::TUnit.Assertions.Should.Core.ShouldAssertion";
}
