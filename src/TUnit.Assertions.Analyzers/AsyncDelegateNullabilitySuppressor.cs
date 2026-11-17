using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Assertions.Analyzers.Helpers;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// Suppresses CS8619 and CS8621 when a lambda or method group that returns <c>Task&lt;T&gt;</c> is passed
/// directly to an <c>Assert.That</c> overload declared with a <c>Func&lt;Task&lt;...&gt;&gt;</c> parameter, for
/// example <c>Assert.That(() =&gt; GetNameAsync())</c> or <c>Assert.That(GetNameAsync)</c>. CS8619 is the warning on
/// a lambda's result; CS8621 the one on converting a method group, or a lambda with an explicit return type, to the
/// delegate.
///
/// The warning is a false positive there: <c>Task&lt;T&gt;</c> is invariant in its nullability, but the
/// assertion only reads the task's result, and a result that cannot be null is always a valid
/// <c>T?</c>. The delegate overloads keep taking <c>Func&lt;Task&lt;T?&gt;&gt;</c> so that awaiting
/// <c>IsNotNull()</c> on a lambda returning <c>Task&lt;T?&gt;</c> still gives a <c>T</c>.
///
/// Only a non-nullable result going to a nullable one is suppressed: a nested difference
/// (<c>Task&lt;List&lt;string&gt;&gt;</c> for <c>Task&lt;List&lt;string?&gt;?&gt;</c>), a task that may itself be null
/// (also through <c>[return: MaybeNull]</c>), and a cast are still reported.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsyncDelegateNullabilitySuppressor : DiagnosticSuppressor
{
    private static readonly SuppressionDescriptor ValueDescriptor = CreateDescriptor("CS8619");
    private static readonly SuppressionDescriptor DelegateReturnDescriptor = CreateDescriptor("CS8621");

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } =
        ImmutableArray.Create(ValueDescriptor, DelegateReturnDescriptor);

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        // Without TUnit.Assertions there is no Assert.That to pass a delegate to.
        var symbols = AssertionSymbols.For(context.Compilation);
        if (symbols.Assert.IsEmpty
            || context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1") is not { } taskOfT)
        {
            return;
        }

        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            var descriptor = diagnostic.Id switch
            {
                "CS8619" => ValueDescriptor,
                "CS8621" => DelegateReturnDescriptor,
                _ => null,
            };

            if (descriptor is null || diagnostic.Location.SourceTree is not { } sourceTree)
            {
                continue;
            }

            var node = sourceTree.GetRoot(context.CancellationToken)
                .FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            var isDelegateConversion = descriptor == DelegateReturnDescriptor;

            // Syntax first: most CS8619/CS8621 in a compilation are nowhere near an Assert.That call.
            if (node is not ExpressionSyntax expression
                || GetThatArgument(expression, isDelegateConversion) is not { } argument)
            {
                continue;
            }

            var semanticModel = context.GetSemanticModel(sourceTree);
            if (IsTaskDelegatePassedToAssertThat(expression, argument, isDelegateConversion, symbols, taskOfT, semanticModel, context.CancellationToken))
            {
                context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
            }
        }
    }

    /// <summary>
    /// The argument of a call to a method named <c>That</c> that <paramref name="expression"/> belongs to: the
    /// converted method group or typed lambda itself, or the lambda whose result it is, with any parentheses around it.
    /// <see langword="null"/> otherwise.
    /// </summary>
    private static ArgumentSyntax? GetThatArgument(ExpressionSyntax expression, bool isDelegateConversion)
    {
        ExpressionSyntax? argumentExpression = isDelegateConversion
            ? (IsMethodGroupOrTypedLambda(expression) ? expression : null)
            : (IsConversionOfItsOwn(expression) ? null : GetLambdaOfResult(expression));
        while (argumentExpression?.Parent is ParenthesizedExpressionSyntax parenthesized)
        {
            argumentExpression = parenthesized;
        }

        return argumentExpression?.Parent is ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } argument
               && IsNamedThat(invocation.Expression)
            ? argument
            : null;
    }

    /// <summary>
    /// The CS8621 forms this suppressor handles: a method group, or a lambda with an explicit return type. A cast of
    /// one is not: the warning then belongs to the cast.
    /// </summary>
    private static bool IsMethodGroupOrTypedLambda(ExpressionSyntax expression)
        => expression is SimpleNameSyntax or MemberAccessExpressionSyntax or AnonymousFunctionExpressionSyntax;

    /// <summary>
    /// True for a result that is a cast or an <c>as</c> conversion (in any parentheses): its own conversion warning
    /// shares the span with the warning on the lambda's result, so the two cannot be told apart.
    /// </summary>
    private static bool IsConversionOfItsOwn(ExpressionSyntax expression)
    {
        var inner = expression;
        while (inner is ParenthesizedExpressionSyntax parenthesized)
        {
            inner = parenthesized.Expression;
        }

        return inner is CastExpressionSyntax || inner.IsKind(SyntaxKind.AsExpression);
    }

    private static bool IsNamedThat(ExpressionSyntax invoked)
    {
        var name = invoked switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            SimpleNameSyntax simpleName => simpleName.Identifier.ValueText,
            _ => null,
        };

        return string.Equals(name, "That", StringComparison.Ordinal);
    }

    /// <summary>
    /// The lambda or anonymous method whose result <paramref name="expression"/> is: its expression body, or the
    /// expression of a <c>return</c> statement of that function, with any parentheses around it. A branch of a
    /// conditional or switch expression is not: its warning is about the conversion to that expression's type.
    /// </summary>
    private static AnonymousFunctionExpressionSyntax? GetLambdaOfResult(ExpressionSyntax expression)
    {
        var result = expression;
        while (result.Parent is ParenthesizedExpressionSyntax parenthesized)
        {
            result = parenthesized;
        }

        if (result.Parent is AnonymousFunctionExpressionSyntax lambda && lambda.ExpressionBody == result)
        {
            return lambda;
        }

        if (result.Parent is ReturnStatementSyntax returnStatement)
        {
            // The return belongs to the nearest enclosing function, which must be the lambda itself.
            return returnStatement.Ancestors()
                .FirstOrDefault(a => a is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or BaseMethodDeclarationSyntax or AccessorDeclarationSyntax)
                as AnonymousFunctionExpressionSyntax;
        }

        return null;
    }

    /// <summary>
    /// True when <paramref name="argument"/> goes to TUnit's <c>Assert.That</c>, to an overload whose parameter is
    /// declared as a delegate returning <c>Task&lt;...&gt;</c>, and the conversion the warning is about takes a task
    /// of a non-nullable result to the same task with that result annotated, and nothing else.
    /// </summary>
    private static bool IsTaskDelegatePassedToAssertThat(
        ExpressionSyntax expression,
        ArgumentSyntax argument,
        bool isDelegateConversion,
        AssertionSymbols symbols,
        INamedTypeSymbol taskOfT,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (argument.Parent?.Parent is not InvocationExpressionSyntax invocation
            || semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method
            || !symbols.IsAssertThat(method)
            || !IsReferencedAssertionAssembly(method.ContainingAssembly, semanticModel.Compilation)
            || GetParameter(method, argument) is not { Type: INamedTypeSymbol { DelegateInvokeMethod: { } invoke } }
            // The overload must be declared with a Task-returning delegate (Func<Task<TValue?>>), not be a
            // Func<TValue?> overload whose TValue happens to be a task.
            || GetParameter(method.OriginalDefinition, argument) is not { Type: INamedTypeSymbol { DelegateInvokeMethod.ReturnType: INamedTypeSymbol declaredReturn } }
            || !IsTaskOfT(declaredReturn, taskOfT))
        {
            return false;
        }

        if (isDelegateConversion)
        {
            // The method group's or typed lambda's return type goes to the delegate's. [return: MaybeNull] makes the
            // task itself possibly null, which the type alone does not show.
            return semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol is IMethodSymbol converted
                   && !HasMaybeNullReturn(converted)
                   && DiffersOnlyInResultAnnotation(converted.ReturnType, invoke.ReturnType, taskOfT);
        }

        // The lambda's result goes to the lambda's return type: the delegate's, or the one a typed lambda declares (whose
        // own conversion to the delegate is the CS8621 above). An implicitly typed lambda's symbol carries the type
        // inferred from its body, so the delegate's is the target there.
        if (GetLambdaOfResult(expression) is not { } lambda)
        {
            return false;
        }

        var resultTarget = invoke.ReturnType;
        if (lambda is ParenthesizedLambdaExpressionSyntax { ReturnType: not null })
        {
            if (semanticModel.GetSymbolInfo(lambda, cancellationToken).Symbol is not IMethodSymbol lambdaSymbol)
            {
                return false;
            }

            resultTarget = lambdaSymbol.ReturnType;
        }

        var typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);

        // A task that may itself be null is a real problem: the assertion would await it.
        if (typeInfo.Nullability.FlowState == NullableFlowState.MaybeNull)
        {
            return false;
        }

        return DiffersOnlyInResultAnnotation(typeInfo.Type, resultTarget, taskOfT);
    }

    private static IParameterSymbol? GetParameter(IMethodSymbol method, ArgumentSyntax argument)
    {
        if (argument.NameColon is { } nameColon)
        {
            return method.Parameters.FirstOrDefault(p => string.Equals(p.Name, nameColon.Name.Identifier.ValueText, StringComparison.Ordinal));
        }

        if (argument.Parent is not BaseArgumentListSyntax argumentList)
        {
            return null;
        }

        var index = argumentList.Arguments.IndexOf(argument);
        return index >= 0 && index < method.Parameters.Length ? method.Parameters[index] : null;
    }

    /// <summary>
    /// True when both are <c>Task&lt;X&gt;</c>, the source task itself cannot be null, the source's <c>X</c> is not
    /// annotated and the target's is (<c>X?</c>), and nothing else differs: the source's <c>X</c> with the annotation
    /// added equals the target's <c>X</c>, nullability included.
    /// </summary>
    private static bool DiffersOnlyInResultAnnotation(ITypeSymbol? source, ITypeSymbol target, INamedTypeSymbol taskOfT)
    {
        if (source is not INamedTypeSymbol { TypeArguments.Length: 1, NullableAnnotation: not NullableAnnotation.Annotated } sourceTask
            || target is not INamedTypeSymbol { TypeArguments.Length: 1 } targetTask
            || !IsTaskOfT(sourceTask, taskOfT)
            || !IsTaskOfT(targetTask, taskOfT))
        {
            return false;
        }

        var sourceResult = sourceTask.TypeArguments[0];
        var targetResult = targetTask.TypeArguments[0];

        return sourceResult.NullableAnnotation != NullableAnnotation.Annotated
            && targetResult.NullableAnnotation == NullableAnnotation.Annotated
            && SymbolEqualityComparer.IncludeNullability.Equals(
                sourceResult.WithNullableAnnotation(NullableAnnotation.Annotated),
                targetResult);
    }

    private static bool IsReferencedAssertionAssembly(IAssemblySymbol? assembly, Compilation compilation)
    {
        // The same guard as IsNotNullAssertionSuppressor: matching names alone also accepts source-defined lookalikes.
        return assembly is not null
               && assembly.Identity.Name is "TUnit.Assertions"
               && !SymbolEqualityComparer.Default.Equals(assembly, compilation.Assembly);
    }

    private static bool IsTaskOfT(INamedTypeSymbol type, INamedTypeSymbol taskOfT)
        => SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, taskOfT);

    private static bool HasMaybeNullReturn(IMethodSymbol method)
        => method.GetReturnTypeAttributes().Any(a => a.AttributeClass is { Name: "MaybeNullAttribute", ContainingNamespace: { Name: "CodeAnalysis", ContainingNamespace: { Name: "Diagnostics", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } } } });

    private static SuppressionDescriptor CreateDescriptor(string id)
        => new(
            id: $"{id}Suppression",
            suppressedDiagnosticId: id,
            justification: $"Suppress {id} for a delegate returning Task<T> passed to Assert.That: the assertion only reads the result, so a non-nullable T is a valid T?."
        );
}
