using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Assertions.Analyzers.Helpers;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// Suppresses CS8619 and CS8621 when a lambda or method group that returns <c>Task&lt;T&gt;</c> is passed
/// directly to an <c>Assert.That</c> overload declared with a <c>Func&lt;Task&lt;...&gt;&gt;</c> parameter, for
/// example <c>Assert.That(() =&gt; GetNameAsync())</c> or <c>Assert.That(GetNameAsync)</c>.
///
/// The warning is a false positive there: <c>Task&lt;T&gt;</c> is invariant in its nullability, but the
/// assertion only reads the task's result, and a result that cannot be null is always a valid
/// <c>T?</c>. The delegate overloads keep taking <c>Func&lt;Task&lt;T?&gt;&gt;</c> so that awaiting
/// <c>IsNotNull()</c> on a lambda returning <c>Task&lt;T?&gt;</c> still gives a <c>T</c>.
///
/// Only a difference in the top-level annotation of the task's result is suppressed: a nested
/// difference (<c>Task&lt;List&lt;string&gt;&gt;</c> for <c>Task&lt;List&lt;string?&gt;?&gt;</c>) is still reported.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsyncDelegateNullabilitySuppressor : DiagnosticSuppressor
{
    private static readonly SuppressionDescriptor ValueDescriptor = CreateDescriptor("CS8619");
    private static readonly SuppressionDescriptor MethodGroupDescriptor = CreateDescriptor("CS8621");

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } =
        ImmutableArray.Create(ValueDescriptor, MethodGroupDescriptor);

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        // Without TUnit.Assertions there is no Assert.That to pass a delegate to.
        var symbols = AssertionSymbols.For(context.Compilation);
        if (symbols.Assert.IsEmpty)
        {
            return;
        }

        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            var descriptor = diagnostic.Id switch
            {
                "CS8619" => ValueDescriptor,
                "CS8621" => MethodGroupDescriptor,
                _ => null,
            };

            if (descriptor is null || diagnostic.Location.SourceTree is not { } sourceTree)
            {
                continue;
            }

            var node = sourceTree.GetRoot(context.CancellationToken)
                .FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            var isMethodGroup = descriptor == MethodGroupDescriptor;

            // Syntax first: most CS8619/CS8621 in a compilation are nowhere near an Assert.That call.
            if (node is not ExpressionSyntax expression
                || GetThatArgument(expression, isMethodGroup) is not { } argument)
            {
                continue;
            }

            var semanticModel = context.GetSemanticModel(sourceTree);
            if (IsTaskDelegatePassedToAssertThat(expression, argument, isMethodGroup, symbols, semanticModel, context.CancellationToken))
            {
                context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
            }
        }
    }

    /// <summary>
    /// The argument of a call to a method named <c>That</c> that <paramref name="expression"/> belongs to: the
    /// method group itself, or the lambda whose result it is, with any parentheses around it.
    /// <see langword="null"/> otherwise.
    /// </summary>
    private static ArgumentSyntax? GetThatArgument(ExpressionSyntax expression, bool isMethodGroup)
    {
        ExpressionSyntax? argumentExpression = isMethodGroup ? expression : GetLambdaOfResult(expression);
        while (argumentExpression?.Parent is ParenthesizedExpressionSyntax parenthesized)
        {
            argumentExpression = parenthesized;
        }

        return argumentExpression?.Parent is ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } argument
               && IsNamedThat(invocation.Expression)
            ? argument
            : null;
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
    /// declared as a delegate returning <c>Task&lt;...&gt;</c>, and the task that <paramref name="expression"/>
    /// gives differs from that parameter's only in the top-level annotation of the result.
    /// </summary>
    private static bool IsTaskDelegatePassedToAssertThat(
        ExpressionSyntax expression,
        ArgumentSyntax argument,
        bool isMethodGroup,
        AssertionSymbols symbols,
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
            || !IsTaskOfT(declaredReturn))
        {
            return false;
        }

        if (isMethodGroup)
        {
            var returnType = (semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol as IMethodSymbol)?.ReturnType;
            return DiffersOnlyInResultAnnotation(returnType, invoke.ReturnType);
        }

        var typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);

        // A task that may itself be null is a real problem: the assertion would await it.
        if (typeInfo.Nullability.FlowState == NullableFlowState.MaybeNull)
        {
            return false;
        }

        return DiffersOnlyInResultAnnotation(typeInfo.Type, invoke.ReturnType);
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
    /// True when both are <c>Task&lt;X&gt;</c> of the same <c>X</c>, the source task itself cannot be null, the
    /// target's <c>X</c> is annotated (<c>X?</c>), and nothing else differs: the source's <c>X</c> with the annotation
    /// added equals the target's <c>X</c>, nullability included.
    /// </summary>
    private static bool DiffersOnlyInResultAnnotation(ITypeSymbol? source, ITypeSymbol target)
    {
        if (source is not INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1, NullableAnnotation: not NullableAnnotation.Annotated } sourceTask
            || target is not INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } targetTask
            || !IsTaskOfT(sourceTask)
            || !SymbolEqualityComparer.Default.Equals(sourceTask.OriginalDefinition, targetTask.OriginalDefinition))
        {
            return false;
        }

        var sourceResult = sourceTask.TypeArguments[0];
        var targetResult = targetTask.TypeArguments[0];

        return targetResult.NullableAnnotation == NullableAnnotation.Annotated
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

    private static bool IsTaskOfT(INamedTypeSymbol type)
        => type.OriginalDefinition is { Name: "Task", Arity: 1, ContainingNamespace: { Name: "Tasks", ContainingNamespace: { Name: "Threading", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } } } };

    private static SuppressionDescriptor CreateDescriptor(string id)
        => new(
            id: $"{id}Suppression",
            suppressedDiagnosticId: id,
            justification: $"Suppress {id} for a delegate returning Task<T> passed to Assert.That: the assertion only reads the result, so a non-nullable T is a valid T?."
        );
}
