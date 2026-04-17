using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// Detects `.IsEqualTo(...)` on collection assertion sources. Because collection
/// types don't override Equals, this uses reference equality. Users almost always
/// want content equivalence via `.IsEquivalentTo(...)`.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CollectionIsEqualToAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.CollectionIsEqualToUsesReferenceEquality);

    public override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
    }

    private void AnalyzeOperation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocation)
        {
            return;
        }

        var method = invocation.TargetMethod;

        if (method.Name != "IsEqualTo")
        {
            return;
        }

        var containingNamespace = method.ContainingType?.ContainingNamespace?.ToDisplayString();
        if (containingNamespace is null || !containingNamespace.StartsWith("TUnit.Assertions"))
        {
            return;
        }

        // Only the generic EqualsAssertion-returning overload uses default (reference) equality.
        // Specialized overloads (CollectionCountEqualsAssertion, DateTimeEqualsAssertion, ...) are fine.
        if (method.ReturnType is not INamedTypeSymbol returnType
            || returnType.Name != "EqualsAssertion")
        {
            return;
        }

        // Determine the type of the source being asserted on. For extension methods,
        // the `this` argument is the first entry in Arguments (even though method.Parameters excludes it).
        // Instance.Type is null for extension methods; Arguments[0].Value.Type gives the actual source type.
        ITypeSymbol? sourceParamType;
        if (method.IsExtensionMethod)
        {
            sourceParamType = invocation.Arguments.Length > 0
                ? invocation.Arguments[0].Value.Type
                : null;
        }
        else
        {
            sourceParamType = invocation.Instance?.Type ?? method.ReceiverType;
        }

        if (sourceParamType is not INamedTypeSymbol sourceType)
        {
            return;
        }

        var assertedType = ExtractAssertionSourceTypeArgument(sourceType);
        if (assertedType is null)
        {
            return;
        }

        if (!IsCollectionWithoutStructuralEquality(assertedType, context.Compilation))
        {
            return;
        }

        var syntax = invocation.Syntax;
        Location reportLocation;
        if (syntax is Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax
            {
                Expression: Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax memberAccess,
            } invocationSyntax)
        {
            var span = TextSpan.FromBounds(memberAccess.Name.SpanStart, invocationSyntax.Span.End);
            reportLocation = Location.Create(syntax.SyntaxTree, span);
        }
        else
        {
            reportLocation = syntax.GetLocation();
        }

        context.ReportDiagnostic(
            Diagnostic.Create(Rules.CollectionIsEqualToUsesReferenceEquality, reportLocation)
        );
    }

    private static ITypeSymbol? ExtractAssertionSourceTypeArgument(INamedTypeSymbol sourceType)
    {
        // Walk the type + its interfaces looking for IAssertionSource<T>.
        foreach (var iface in sourceType.AllInterfaces.Concat(new[] { sourceType }))
        {
            if (iface.Name == "IAssertionSource"
                && iface.ContainingNamespace?.ToDisplayString() == "TUnit.Assertions.Core"
                && iface.TypeArguments.Length == 1)
            {
                return iface.TypeArguments[0];
            }
        }
        return null;
    }

    private static bool IsCollectionWithoutStructuralEquality(ITypeSymbol type, Compilation compilation)
    {
        if (type.SpecialType == SpecialType.System_String)
        {
            return false;
        }

        var unconstructedName = (type as INamedTypeSymbol)?.ConstructedFrom?.ToDisplayString();
        if (unconstructedName is "System.Memory<T>"
            or "System.ReadOnlyMemory<T>"
            or "System.Span<T>"
            or "System.ReadOnlySpan<T>")
        {
            return false;
        }

        var ienumerable = compilation.GetTypeByMetadataName("System.Collections.IEnumerable");
        if (ienumerable is null)
        {
            return false;
        }

        if (SymbolEqualityComparer.Default.Equals(type, ienumerable))
        {
            return true;
        }

        foreach (var iface in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface, ienumerable))
            {
                return true;
            }
        }

        return false;
    }
}
