using System.Text;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for IAsyncEnumerable values.
/// This is the entry point for: Assert.That(asyncEnumerable)
/// Enables async sequence validation without blocking.
/// </summary>
public class AsyncEnumerableAssertion<TItem> : IAssertionSource<IAsyncEnumerable<TItem>>
{
    public AssertionContext<IAsyncEnumerable<TItem>> Context { get; }

    public AsyncEnumerableAssertion(IAsyncEnumerable<TItem>? value, string? expression)
    {
        var expressionBuilder = new StringBuilder();
        expressionBuilder.Append($"Assert.That({expression ?? "?"})");
        Context = new AssertionContext<IAsyncEnumerable<TItem>>(value, expressionBuilder);
    }

    /// <summary>
    /// Protected constructor for derived classes that need to pass an existing context.
    /// </summary>
    protected AsyncEnumerableAssertion(AssertionContext<IAsyncEnumerable<TItem>> context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Constructor for continuation classes (AndContinuation, OrContinuation).
    /// Handles linking to previous assertion and appending combiner expression.
    /// Private protected means accessible only to derived classes within the same assembly.
    /// </summary>
    private protected AsyncEnumerableAssertion(
        AssertionContext<IAsyncEnumerable<TItem>> context,
        Assertion<IAsyncEnumerable<TItem>> previousAssertion,
        string combinerExpression,
        CombinerType combinerType)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        context.ExpressionBuilder.Append(combinerExpression);
        context.SetPendingLink(previousAssertion, combinerType);
    }

    /// <summary>
    /// Asserts that the value is of the specified type and returns an assertion on the casted value.
    /// </summary>
    public TypeOfAssertion<IAsyncEnumerable<TItem>, TExpected> IsTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<IAsyncEnumerable<TItem>, TExpected>(Context);
    }

    /// <summary>
    /// Asserts that the value's type is assignable to the specified type.
    /// </summary>
    public IsAssignableToAssertion<TTarget, IAsyncEnumerable<TItem>> IsAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableTo<{typeof(TTarget).Name}>()");
        return new IsAssignableToAssertion<TTarget, IAsyncEnumerable<TItem>>(Context);
    }

    /// <summary>
    /// Asserts that the value's type is NOT assignable to the specified type.
    /// </summary>
    public IsNotAssignableToAssertion<TTarget, IAsyncEnumerable<TItem>> IsNotAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableTo<{typeof(TTarget).Name}>()");
        return new IsNotAssignableToAssertion<TTarget, IAsyncEnumerable<TItem>>(Context);
    }

    /// <summary>
    /// Asserts that the value is NOT of the specified type.
    /// </summary>
    public IsNotTypeOfAssertion<IAsyncEnumerable<TItem>, TExpected> IsNotTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsNotTypeOf<{typeof(TExpected).Name}>()");
        return new IsNotTypeOfAssertion<IAsyncEnumerable<TItem>, TExpected>(Context);
    }
}
