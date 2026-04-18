using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        context.RegisterCompilationStartAction(compilationStart =>
        {
            var ienumerable = compilationStart.Compilation.GetTypeByMetadataName("System.Collections.IEnumerable");
            if (ienumerable is null)
            {
                return;
            }

            compilationStart.RegisterOperationAction(
                ctx => AnalyzeOperation(ctx, ienumerable),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzeOperation(OperationAnalysisContext context, INamedTypeSymbol ienumerable)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        if (method.Name != "IsEqualTo")
        {
            return;
        }

        // Only the generic EqualsAssertion-returning overload uses default (reference) equality.
        // Specialized overloads (CollectionCountEqualsAssertion, DateTimeEqualsAssertion, ...) are fine.
        if (method.ReturnType is not INamedTypeSymbol { Name: "EqualsAssertion" })
        {
            return;
        }

        var containingNamespace = method.ContainingType?.ContainingNamespace?.ToDisplayString();
        if (containingNamespace is null || !containingNamespace.StartsWith("TUnit.Assertions"))
        {
            return;
        }

        // For extension methods, Instance.Type is null; the `this` argument is Arguments[0]
        // even though method.Parameters excludes it.
        var sourceParamType = method.IsExtensionMethod
            ? (invocation.Arguments.Length > 0 ? invocation.Arguments[0].Value.Type : null)
            : invocation.Instance?.Type ?? method.ReceiverType;

        if (sourceParamType is not INamedTypeSymbol sourceType)
        {
            return;
        }

        var assertedType = ExtractAssertionSourceTypeArgument(sourceType);
        if (assertedType is null)
        {
            return;
        }

        if (!IsCollectionWithoutStructuralEquality(assertedType, ienumerable))
        {
            return;
        }

        var syntax = invocation.Syntax;
        var reportLocation = syntax is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax memberAccess,
            } invocationSyntax
            ? Location.Create(
                syntax.SyntaxTree,
                TextSpan.FromBounds(memberAccess.Name.SpanStart, invocationSyntax.Span.End))
            : syntax.GetLocation();

        context.ReportDiagnostic(
            Diagnostic.Create(Rules.CollectionIsEqualToUsesReferenceEquality, reportLocation)
        );
    }

    private static ITypeSymbol? ExtractAssertionSourceTypeArgument(INamedTypeSymbol sourceType)
    {
        if (TryGetAssertionSourceArg(sourceType, out var arg))
        {
            return arg;
        }

        foreach (var iface in sourceType.AllInterfaces)
        {
            if (TryGetAssertionSourceArg(iface, out arg))
            {
                return arg;
            }
        }

        return null;
    }

    private static bool TryGetAssertionSourceArg(INamedTypeSymbol type, out ITypeSymbol? arg)
    {
        if (type.Name == "IAssertionSource"
            && type.ContainingNamespace?.ToDisplayString() == "TUnit.Assertions.Core"
            && type.TypeArguments.Length == 1)
        {
            arg = type.TypeArguments[0];
            return true;
        }

        arg = null;
        return false;
    }

    private static bool IsCollectionWithoutStructuralEquality(ITypeSymbol type, INamedTypeSymbol ienumerable)
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

        if (!ImplementsIEnumerable(type, ienumerable))
        {
            return false;
        }

        // Records synthesize an Equals(object) override; custom collections may too.
        // In those cases IsEqualTo is semantically correct and should not be flagged.
        return !OverridesObjectEquals(type);
    }

    private static bool ImplementsIEnumerable(ITypeSymbol type, INamedTypeSymbol ienumerable)
    {
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

    private static bool OverridesObjectEquals(ITypeSymbol type)
    {
        for (var current = type;
             current is not null && current.SpecialType != SpecialType.System_Object;
             current = current.BaseType)
        {
            foreach (var member in current.GetMembers("Equals"))
            {
                if (member is IMethodSymbol { IsOverride: true, Parameters.Length: 1 } m
                    && m.Parameters[0].Type.SpecialType == SpecialType.System_Object)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
