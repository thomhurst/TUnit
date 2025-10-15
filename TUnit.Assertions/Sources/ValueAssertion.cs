using System.Text;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for immediate values.
/// This is the entry point for: Assert.That(value)
/// Does not inherit from Assertion to prevent premature awaiting.
/// </summary>
public class ValueAssertion<TValue> : IAssertionSource<TValue>
{
    public AssertionContext<TValue> Context { get; }

    public ValueAssertion(TValue? value, string? expression)
    {
        var expressionBuilder = new StringBuilder();
        expressionBuilder.Append($"Assert.That({expression ?? "?"})");
        Context = new AssertionContext<TValue>(value, expressionBuilder);
    }

    /// <summary>
    /// Asserts that the value is of the specified type and returns an assertion on the casted value.
    /// This instance method allows single type parameter usage without needing to specify the source type.
    /// Example: await Assert.That(myList).IsTypeOf<List<string>>();
    /// </summary>
    public TypeOfAssertion<TValue, TExpected> IsTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<TValue, TExpected>(Context);
    }

    /// <summary>
    /// Asserts that the value's type is assignable to the specified type.
    /// This instance method allows single type parameter usage without needing to specify the source type.
    /// Example: await Assert.That(myObject).IsAssignableTo<IDisposable>();
    /// </summary>
    public IsAssignableToAssertion<TTarget, TValue> IsAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableTo<{typeof(TTarget).Name}>()");
        return new IsAssignableToAssertion<TTarget, TValue>(Context);
    }

    /// <summary>
    /// Asserts that the value's type is NOT assignable to the specified type.
    /// This instance method allows single type parameter usage without needing to specify the source type.
    /// Example: await Assert.That(myObject).IsNotAssignableTo<IDisposable>();
    /// </summary>
    public IsNotAssignableToAssertion<TTarget, TValue> IsNotAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableTo<{typeof(TTarget).Name}>()");
        return new IsNotAssignableToAssertion<TTarget, TValue>(Context);
    }
}
