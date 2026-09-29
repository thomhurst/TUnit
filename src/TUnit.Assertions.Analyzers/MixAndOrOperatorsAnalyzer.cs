using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using TUnit.Assertions.Analyzers.Helpers;

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
        context.RegisterCompilationStartAction(compilationStart =>
        {
            var symbols = AssertionSymbols.For(compilationStart.Compilation);

            if (symbols.IAssertionSource.IsEmpty && symbols.IShouldSource.IsEmpty && symbols.AssertionOfT.IsEmpty)
            {
                return;
            }

            compilationStart.RegisterOperationAction(ctx => AnalyzeOperation(ctx, symbols), OperationKind.Await);
        });
    }

    private static void AnalyzeOperation(OperationAnalysisContext context, AssertionSymbols symbols)
    {
        if (context.Operation is not IAwaitOperation awaitOperation)
        {
            return;
        }

        var hasAnd = false;
        var hasOr = false;

        // Walk only the awaited chain's receivers (Assert.That(x).A().And.B().Or.C()), not arguments or
        // lambdas: an assertion nested inside an argument is a separate chain, evaluated on its own.
        for (var current = awaitOperation.Operation; current is not null;)
        {
            switch (current)
            {
                case IPropertyReferenceOperation propertyReference:
                    hasAnd |= propertyReference.Property.Name == "And";
                    hasOr |= propertyReference.Property.Name == "Or";
                    current = propertyReference.Instance;
                    break;
                case IInvocationOperation invocation:
                    current = symbols.IsShould(invocation.TargetMethod) ? null : GetReceiver(invocation);
                    break;
                case IConversionOperation conversion:
                    current = conversion.Operand;
                    break;
                case IParenthesizedOperation parenthesized:
                    current = parenthesized.Operand;
                    break;
                default:
                    current = null;
                    break;
            }
        }

        if (!hasAnd || !hasOr)
        {
            return;
        }

        // Check if the awaited type implements IAssertionSource<T>/IShouldSource<T> or
        // inherits from Assertion<T>. ShouldAssertion<T> is covered by the IShouldSource branch
        // (it implements that interface), so it doesn't need a separate base-type check.
        // Done after the chain walk because AllInterfaces is comparatively expensive for the
        // constructed generic assertion types, and almost no awaited chain mixes And with Or.
        if (awaitOperation.Operation.Type is { } awaitedType
            && (IsAssertionSource(awaitedType, symbols) || IsAssertionType(awaitedType.BaseType, symbols.AssertionOfT)))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MixAndOrConditionsAssertion, awaitOperation.Syntax.GetLocation()));
        }
    }

    private static IOperation? GetReceiver(IInvocationOperation invocation)
    {
        if (invocation.Instance is { } instance)
        {
            return instance;
        }

        // Extension methods take their receiver as the `this` argument.
        if (invocation.TargetMethod.IsExtensionMethod)
        {
            foreach (var argument in invocation.Arguments)
            {
                if (argument.Parameter?.Ordinal == 0)
                {
                    return argument.Value;
                }
            }
        }

        return null;
    }

    private static bool IsAssertionSource(ITypeSymbol type, AssertionSymbols symbols)
    {
        foreach (var @interface in type.AllInterfaces)
        {
            var definition = @interface.OriginalDefinition;

            if (symbols.IAssertionSource.Contains(definition)
                || symbols.IAssertionSourceOfT.Contains(definition)
                || symbols.IShouldSource.Contains(definition)
                || symbols.IShouldSourceOfT.Contains(definition))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAssertionType(INamedTypeSymbol? type, TypeSymbolSet assertionOfT)
    {
        if (assertionOfT.IsEmpty)
        {
            return false;
        }

        for (; type is not null; type = type.BaseType)
        {
            if (assertionOfT.Contains(type.OriginalDefinition))
            {
                return true;
            }
        }

        return false;
    }
}
