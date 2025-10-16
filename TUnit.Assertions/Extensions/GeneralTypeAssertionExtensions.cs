using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Extension methods for general type assertions (IsTypeOf, IsAssignableTo, etc.) that target IAssertionSource.
/// These methods work on any class that implements IAssertionSource&lt;TValue&gt;,
/// including regular assertions and their And/Or continuations.
/// </summary>
public static class GeneralTypeAssertionExtensions
{
    /// <summary>
    /// Asserts that the value is of the specified type and returns an assertion on the casted value.
    /// This extension method allows single type parameter usage without needing to specify the source type.
    /// Example: await Assert.That(value).IsNotNull().And.IsTypeOf&lt;List&lt;string&gt;&gt;();
    /// </summary>
    public static TypeOfAssertion<TValue, TExpected> IsTypeOf<TValue, TExpected>(
        this IAssertionSource<TValue> source)
    {
        source.Context.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<TValue, TExpected>(source.Context);
    }

    /// <summary>
    /// Asserts that the value's type is assignable to the specified type.
    /// This extension method allows single type parameter usage without needing to specify the source type.
    /// Example: await Assert.That(value).IsNotNull().And.IsAssignableTo&lt;IDisposable&gt;();
    /// </summary>
    public static IsAssignableToAssertion<TTarget, TValue> IsAssignableTo<TTarget, TValue>(
        this IAssertionSource<TValue> source)
    {
        source.Context.ExpressionBuilder.Append($".IsAssignableTo<{typeof(TTarget).Name}>()");
        return new IsAssignableToAssertion<TTarget, TValue>(source.Context);
    }

    /// <summary>
    /// Asserts that the value's type is NOT assignable to the specified type.
    /// This extension method allows single type parameter usage without needing to specify the source type.
    /// Example: await Assert.That(value).IsNotNull().And.IsNotAssignableTo&lt;IDisposable&gt;();
    /// </summary>
    public static IsNotAssignableToAssertion<TTarget, TValue> IsNotAssignableTo<TTarget, TValue>(
        this IAssertionSource<TValue> source)
    {
        source.Context.ExpressionBuilder.Append($".IsNotAssignableTo<{typeof(TTarget).Name}>()");
        return new IsNotAssignableToAssertion<TTarget, TValue>(source.Context);
    }
}
