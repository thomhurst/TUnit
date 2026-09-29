using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Assertions.Analyzers.Extensions;
using TUnit.Assertions.Analyzers.Helpers;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// Suppresses nullability warnings (CS8600, CS8602, CS8604, CS8618, CS8629) for variables
/// after they have been asserted as non-null using Assert.That(x).IsNotNull()
/// or x.Should().NotBeNull().
///
/// Note: This suppressor only hides the warnings; it does not change the compiler's
/// null-state flow analysis. Variables will still appear as nullable in IntelliSense.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class IsNotNullAssertionSuppressor : DiagnosticSuppressor
{
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        // Diagnostics cluster in the same methods, so the candidate null checks of each scope are collected
        // (and semantically validated) once per call rather than once per diagnostic.
        Dictionary<SyntaxNode, List<NullCheckCandidate>>? candidatesByScope = null;
        NullCheckTypes? nullCheckTypes = null;

        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            // Only process nullability warnings
            if (!IsNullabilityWarning(diagnostic.Id))
            {
                continue;
            }

            // Get the syntax tree and semantic model
            if (diagnostic.Location.SourceTree is not { } sourceTree)
            {
                continue;
            }

            var root = sourceTree.GetRoot(context.CancellationToken);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);

            if (node is null)
            {
                continue;
            }

            // Find the variable/expression being referenced that caused the warning
            var targetExpression = GetTargetExpression(node);
            if (targetExpression is null)
            {
                continue;
            }

            var semanticModel = context.GetSemanticModel(sourceTree);
            nullCheckTypes ??= new NullCheckTypes(semanticModel.Compilation);
            candidatesByScope ??= new Dictionary<SyntaxNode, List<NullCheckCandidate>>();

            // Check if this variable/expression was previously asserted as non-null
            if (WasAssertedNotNull(targetExpression, semanticModel, nullCheckTypes, candidatesByScope, context.CancellationToken))
            {
                Suppress(context, diagnostic);
            }
        }
    }

    private bool IsNullabilityWarning(string diagnosticId)
    {
        return diagnosticId is "CS8600" // Converting null literal or possible null value to non-nullable type
            or "CS8602" // Dereference of a possibly null reference
            or "CS8604" // Possible null reference argument
            or "CS8618" // Non-nullable field/property uninitialized
            or "CS8629"; // Nullable value type may be null
    }

    private ExpressionSyntax? GetTargetExpression(SyntaxNode node)
    {
        // The warning might be on the identifier itself, a member access, or a parent node
        return node switch
        {
            IdentifierNameSyntax identifier => identifier,
            MemberAccessExpressionSyntax memberAccess => memberAccess,
            ArgumentSyntax { Expression: var expression } => expression,
            _ => node.DescendantNodesAndSelf()
                .OfType<ExpressionSyntax>()
                .FirstOrDefault(e => e is IdentifierNameSyntax or MemberAccessExpressionSyntax)
        };
    }

    // Statement-order match only — not control-flow aware. An assertion inside an `if (cond)` or
    // `try`/`catch` branch suppresses warnings on subsequent uses even when the assertion may not
    // have run on every path. Accepting that imprecision keeps the analyzer cheap; the alternative
    // (full dataflow analysis via Roslyn's IFlowAnalysis) is significant complexity for a niche
    // false-suppression case. See AwaitAssertionAnalyzer for the symmetric awaitedness check.
    private bool WasAssertedNotNull(
        ExpressionSyntax targetExpression,
        SemanticModel semanticModel,
        NullCheckTypes nullCheckTypes,
        Dictionary<SyntaxNode, List<NullCheckCandidate>> candidatesByScope,
        CancellationToken cancellationToken)
    {
        // Find the innermost containing scope (lambda, local function, or method)
        SyntaxNode? containingMethod = null;
        foreach (var ancestor in targetExpression.Ancestors())
        {
            if (ancestor is MethodDeclarationSyntax
                or LocalFunctionStatementSyntax
                or AnonymousFunctionExpressionSyntax)
            {
                containingMethod = ancestor;
                break;
            }
        }

        if (containingMethod is null)
        {
            return false;
        }

        // Look for Assert.That(variable).IsNotNull() patterns before this usage
        var identifierStatement = targetExpression.FirstAncestorOrSelf<StatementSyntax>();

        if (identifierStatement is null || !IsStrictDescendantOf(identifierStatement, containingMethod))
        {
            return false;
        }

        if (!candidatesByScope.TryGetValue(containingMethod, out var candidates))
        {
            candidates = CollectCandidates(containingMethod);
            candidatesByScope.Add(containingMethod, candidates);
        }

        // Semantically this checks every invocation inside any statement of the scope that precedes the
        // usage's statement in document (pre-)order, which includes the statements that enclose it.
        // Rather than scanning the descendants of each such statement (quadratic in nesting depth),
        // each invocation is visited once and tested by its outermost enclosing statement instead: an
        // invocation is inside some preceding statement exactly when its outermost statement within the
        // scope precedes (or encloses) the usage's statement.
        foreach (var candidate in candidates)
        {
            if (candidate.OutermostStatement == identifierStatement
                || candidate.OutermostStatement.SpanStart > identifierStatement.SpanStart)
            {
                continue;
            }

            // Look for await Assert.That(x).IsNotNull() pattern
            if (candidate.GetAssertedExpression(semanticModel, nullCheckTypes, cancellationToken) is { } assertedExpression
                && ExpressionsMatch(assertedExpression, targetExpression, semanticModel, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static List<NullCheckCandidate> CollectCandidates(SyntaxNode scope)
    {
        var candidates = new List<NullCheckCandidate>();

        foreach (var node in scope.DescendantNodes())
        {
            if (node is InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "IsNotNull" or "NotBeNull" }
                } invocation
                && GetOutermostStatement(invocation, scope) is { } outermostStatement)
            {
                candidates.Add(new NullCheckCandidate(invocation, outermostStatement));
            }
        }

        return candidates;
    }

    private static bool IsStrictDescendantOf(SyntaxNode node, SyntaxNode ancestor)
    {
        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            if (current == ancestor)
            {
                return true;
            }
        }

        return false;
    }

    private static StatementSyntax? GetOutermostStatement(SyntaxNode node, SyntaxNode scope)
    {
        StatementSyntax? outermost = null;

        for (var current = node.Parent; current is not null && current != scope; current = current.Parent)
        {
            if (current is StatementSyntax statement)
            {
                outermost = statement;
            }
        }

        return outermost;
    }

    /// <summary>
    /// Returns the expression a recognised TUnit null check asserts on, or null when
    /// <paramref name="invocation"/> isn't one.
    /// </summary>
    private static ExpressionSyntax? GetAssertedExpression(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        NullCheckTypes nullCheckTypes,
        CancellationToken cancellationToken)
    {
        // Patterns recognised:
        //   await Assert.That(variable).IsNotNull()
        //   await Assert.That(variable).Contains("test").And.IsNotNull()
        //   Assert.That(variable).IsNotNull().GetAwaiter().GetResult()
        //   await variable.Should().NotBeNull()
        //   await variable.Should().Contain("test").And.NotBeNull()
        if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: var calledName })
        {
            return null;
        }

        return calledName switch
        {
            "IsNotNull" => GetAssertThatArgument(invocation, semanticModel, nullCheckTypes, cancellationToken),
            "NotBeNull" => GetShouldReceiver(invocation, semanticModel, cancellationToken),
            _ => null,
        };
    }

    private static ExpressionSyntax? GetAssertThatArgument(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        NullCheckTypes nullCheckTypes,
        CancellationToken cancellationToken)
    {
        var assertThatCall = FindAssertThatInChain(invocation);
        if (assertThatCall is null
            || assertThatCall.ArgumentList.Arguments.Count != 1
            || !IsSupportedAssertionChain(invocation, assertThatCall, semanticModel, cancellationToken)
            || !IsTUnitIsNotNullMethod(invocation, semanticModel, nullCheckTypes, cancellationToken)
            || !IsTUnitMethod(
                assertThatCall,
                semanticModel,
                cancellationToken,
                "global::TUnit.Assertions.Assert",
                "That"))
        {
            return null;
        }

        return assertThatCall.ArgumentList.Arguments[0].Expression;
    }

    private static bool IsTUnitIsNotNullMethod(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        NullCheckTypes nullCheckTypes,
        CancellationToken cancellationToken)
    {
        if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol symbol)
        {
            return false;
        }

        var method = symbol.ReducedFrom ?? symbol;

        if (method.Name != "IsNotNull"
            || !IsReferencedAssertionAssembly(method.ContainingAssembly, semanticModel.Compilation))
        {
            return false;
        }

        if (method.ContainingType.IsGloballyQualifiedNonGeneric("global::TUnit.Assertions.Extensions.AssertionExtensions"))
        {
            return true;
        }

        var collectionBase = nullCheckTypes.CollectionBase;

        // Check the declaring assembly as well as the shared base: a custom subclass
        // can hide IsNotNull, but that does not make its method a TUnit null check.
        if (!collectionBase.ContainsAssembly(method.ContainingAssembly))
        {
            return false;
        }

        var asyncEnumerableBase = nullCheckTypes.AsyncEnumerableBase;
        var asyncDelegate = nullCheckTypes.AsyncDelegate;

        for (var type = method.ContainingType; type is not null; type = type.BaseType)
        {
            var definition = type.OriginalDefinition;

            if (collectionBase.Contains(definition)
                || asyncEnumerableBase.Contains(definition)
                || asyncDelegate.Contains(definition))
            {
                return true;
            }
        }

        return false;
    }

    private static ExpressionSyntax? GetShouldReceiver(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (!IsTUnitMethod(
                invocation,
                semanticModel,
                cancellationToken,
                "global::TUnit.Assertions.Should.Extensions.ShouldAssertionExtensions",
                "NotBeNull"))
        {
            return null;
        }

        var shouldCall = FindShouldInChain(invocation);
        if (shouldCall is null
            || !IsSupportedAssertionChain(invocation, shouldCall, semanticModel, cancellationToken)
            || !IsTUnitMethod(
                shouldCall,
                semanticModel,
                cancellationToken,
                "global::TUnit.Assertions.Should.ShouldExtensions",
                "Should"))
        {
            return null;
        }

        // Should is an extension method — its receiver is the value being asserted.
        return shouldCall.Expression is MemberAccessExpressionSyntax memberAccess
            ? memberAccess.Expression
            : null;
    }

    private static bool IsSupportedAssertionChain(
        InvocationExpressionSyntax nullCheck,
        InvocationExpressionSyntax entryPoint,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        ExpressionSyntax outermost = nullCheck;
        while ((outermost.Parent is MemberAccessExpressionSyntax member && member.Expression == outermost)
               || (outermost.Parent is InvocationExpressionSyntax call && call.Expression == outermost)
               || outermost.Parent is ParenthesizedExpressionSyntax)
        {
            outermost = (ExpressionSyntax)outermost.Parent;
        }

        // Or on either side makes the null check optional. Walk only the receiver
        // chain, not nested arguments or lambdas belonging to another assertion.
        for (ExpressionSyntax? current = outermost;
             current is not null && current != entryPoint;
             current = GetChainReceiver(current))
        {
            if (current is MemberAccessExpressionSyntax { Name.Identifier.Text: "Or" })
            {
                return false;
            }
        }

        // An external method/property can return a TUnit assertion on another value.
        // Do not trace a null check back across such a transformation.
        for (ExpressionSyntax? current = nullCheck; current is not null; current = GetChainReceiver(current))
        {
            if (current == entryPoint)
            {
                return true;
            }

            if (current is not ParenthesizedExpressionSyntax)
            {
                var symbol = semanticModel.GetSymbolInfo(current, cancellationToken).Symbol;
                if (!IsReferencedAssertionAssembly(symbol?.ContainingAssembly, semanticModel.Compilation))
                {
                    return false;
                }
            }
        }

        return false;
    }

    private static ExpressionSyntax? GetChainReceiver(ExpressionSyntax expression) => expression switch
    {
        InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } => member.Expression,
        MemberAccessExpressionSyntax member => member.Expression,
        ParenthesizedExpressionSyntax parenthesized => parenthesized.Expression,
        _ => null,
    };

    private static bool IsTUnitMethod(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        string fullyQualifiedContainingTypeName,
        string methodName)
    {
        if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol symbol)
        {
            return false;
        }

        var method = symbol.ReducedFrom ?? symbol;
        return method.Name == methodName
               && IsReferencedAssertionAssembly(method.ContainingAssembly, semanticModel.Compilation)
               && method.ContainingType.IsGloballyQualifiedNonGeneric(fullyQualifiedContainingTypeName);
    }

    private static bool IsReferencedAssertionAssembly(IAssemblySymbol? assembly, Compilation compilation)
    {
        // Matching namespace/type names alone also accepts source-defined lookalikes.
        return assembly is not null
               && (assembly.Identity.Name is "TUnit.Assertions" or "TUnit.Assertions.Should")
               && !SymbolEqualityComparer.Default.Equals(assembly, compilation.Assembly);
    }

    private static bool ExpressionsMatch(
        ExpressionSyntax assertArgument,
        ExpressionSyntax targetExpression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // For simple identifiers, compare using semantic symbols (handles renames, etc.)
        if (assertArgument is IdentifierNameSyntax && targetExpression is IdentifierNameSyntax)
        {
            return SymbolsMatch(assertArgument, targetExpression, semanticModel, cancellationToken);
        }

        // For member access chains (e.g., value.Id), recursively compare member and receiver
        if (assertArgument is MemberAccessExpressionSyntax assertMember &&
            targetExpression is MemberAccessExpressionSyntax targetMember)
        {
            return SymbolsMatch(assertMember, targetMember, semanticModel, cancellationToken) &&
                   ExpressionsMatch(assertMember.Expression, targetMember.Expression, semanticModel, cancellationToken);
        }

        // Mismatched expression types (e.g., identifier vs member access) are intentionally
        // not matched — asserting `id` should not suppress warnings on `wrapper.Id` or vice versa.
        return false;
    }

    private static bool SymbolsMatch(
        ExpressionSyntax expr1,
        ExpressionSyntax expr2,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var symbol1 = semanticModel.GetSymbolInfo(expr1, cancellationToken).Symbol;
        var symbol2 = semanticModel.GetSymbolInfo(expr2, cancellationToken).Symbol;
        return symbol1 is not null && SymbolEqualityComparer.Default.Equals(symbol1, symbol2);
    }

    private static InvocationExpressionSyntax? FindAssertThatInChain(InvocationExpressionSyntax invocation)
        => FindInvocationInChain(invocation, identifierName: "That", parentName: "Assert");

    // Should() is an extension method, so its receiver is the asserted value (any expression).
    // parentName MUST stay null because the receiver is the asserted value. Semantic validation
    // in GetShouldReceiver still ensures that only TUnit's Should extension qualifies.
    private static InvocationExpressionSyntax? FindShouldInChain(InvocationExpressionSyntax invocation)
        => FindInvocationInChain(invocation, identifierName: "Should", parentName: null);

    /// <summary>
    /// Walks up an expression chain looking for an invocation whose member-access name is
    /// <paramref name="identifierName"/>. When <paramref name="parentName"/> is non-null the
    /// invocation must also be of the form <c>{parentName}.{identifierName}(...)</c>; for
    /// extension methods (<c>Should</c>) the receiver is arbitrary so parentName is null.
    /// </summary>
    private static InvocationExpressionSyntax? FindInvocationInChain(
        InvocationExpressionSyntax invocation,
        string identifierName,
        string? parentName)
    {
        var current = invocation.Expression;

        while (current is not null)
        {
            if (current is InvocationExpressionSyntax invocationExpr)
            {
                if (invocationExpr.Expression is MemberAccessExpressionSyntax memberExpr
                    && memberExpr.Name.Identifier.Text == identifierName
                    && (parentName is null
                        || (memberExpr.Expression is IdentifierNameSyntax id && id.Identifier.Text == parentName)))
                {
                    return invocationExpr;
                }

                current = invocationExpr.Expression;
            }
            else if (current is MemberAccessExpressionSyntax memberAccess)
            {
                current = memberAccess.Expression;
            }
            else
            {
                break;
            }
        }

        return null;
    }

    /// <summary>
    /// An <c>IsNotNull()</c>/<c>NotBeNull()</c> invocation in a scope, with its semantic validation memoized
    /// so it runs at most once however many diagnostics in the scope it is checked against.
    /// </summary>
    private sealed class NullCheckCandidate(InvocationExpressionSyntax invocation, StatementSyntax outermostStatement)
    {
        private bool _resolved;
        private ExpressionSyntax? _assertedExpression;

        public StatementSyntax OutermostStatement { get; } = outermostStatement;

        public ExpressionSyntax? GetAssertedExpression(
            SemanticModel semanticModel,
            NullCheckTypes nullCheckTypes,
            CancellationToken cancellationToken)
        {
            if (!_resolved)
            {
                _assertedExpression = IsNotNullAssertionSuppressor.GetAssertedExpression(
                    invocation, semanticModel, nullCheckTypes, cancellationToken);
                _resolved = true;
            }

            return _assertedExpression;
        }
    }

    /// <summary>
    /// The TUnit.Assertions source base types whose <c>IsNotNull</c> members count as null checks,
    /// resolved on first use and then reused for the rest of the <see cref="ReportSuppressions"/> call.
    /// </summary>
    private sealed class NullCheckTypes(Compilation compilation)
    {
        private bool _resolved;
        private TypeSymbolSet _collectionBase;
        private TypeSymbolSet _asyncEnumerableBase;
        private TypeSymbolSet _asyncDelegate;

        public TypeSymbolSet CollectionBase
        {
            get
            {
                EnsureResolved();
                return _collectionBase;
            }
        }

        public TypeSymbolSet AsyncEnumerableBase
        {
            get
            {
                EnsureResolved();
                return _asyncEnumerableBase;
            }
        }

        public TypeSymbolSet AsyncDelegate
        {
            get
            {
                EnsureResolved();
                return _asyncDelegate;
            }
        }

        private void EnsureResolved()
        {
            if (_resolved)
            {
                return;
            }

            var hasExternAliasedReferences = TypeSymbolSet.HasExternAliasedReferences(compilation);
            _collectionBase = TypeSymbolSet.Resolve(compilation, "TUnit.Assertions.Sources.CollectionAssertionBase`2", hasExternAliasedReferences);
            _asyncEnumerableBase = TypeSymbolSet.Resolve(compilation, "TUnit.Assertions.Sources.AsyncEnumerableAssertionBase`1", hasExternAliasedReferences);
            _asyncDelegate = TypeSymbolSet.Resolve(compilation, "TUnit.Assertions.Sources.AsyncDelegateAssertion", hasExternAliasedReferences);
            _resolved = true;
        }
    }

    private void Suppress(SuppressionAnalysisContext context, Diagnostic diagnostic)
    {
        if (SuppressionsByDiagnosticId.TryGetValue(diagnostic.Id, out var suppression))
        {
            context.ReportSuppression(
                Suppression.Create(
                    suppression,
                    diagnostic
                )
            );
        }
    }

    private static readonly SuppressionDescriptor[] Descriptors =
    [
        CreateDescriptor("CS8600"),
        CreateDescriptor("CS8602"),
        CreateDescriptor("CS8604"),
        CreateDescriptor("CS8618"),
        CreateDescriptor("CS8629"),
    ];

    private static readonly Dictionary<string, SuppressionDescriptor> SuppressionsByDiagnosticId =
        Descriptors.ToDictionary(d => d.SuppressedDiagnosticId);

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } =
        ImmutableArray.Create(Descriptors);

    private static SuppressionDescriptor CreateDescriptor(string id)
        => new(
            id: $"{id}Suppression",
            suppressedDiagnosticId: id,
            justification: $"Suppress {id} for variables asserted as non-null via Assert.That(x).IsNotNull() or x.Should().NotBeNull()."
        );
}
