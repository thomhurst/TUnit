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
        // Initialize StringBuilder with enough space for expression and text
        var expressionBuilder = new StringBuilder((expression?.Length ?? 1) + 32);
        expressionBuilder.Append($"Assert.That({expression ?? "?"})");
        Context = new AssertionContext<TValue>(value, expressionBuilder);
    }

    /// <summary>
    /// Protected constructor for derived classes that need to pass an existing context.
    /// </summary>
    protected ValueAssertion(AssertionContext<TValue> context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Constructor for continuation classes (AndContinuation, OrContinuation).
    /// Handles linking to previous assertion and appending combiner expression.
    /// Private protected means accessible only to derived classes within the same assembly.
    /// </summary>
    private protected ValueAssertion(
        AssertionContext<TValue> context,
        Assertion<TValue> previousAssertion,
        string combinerExpression,
        CombinerType combinerType)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        context.ExpressionBuilder.Append(combinerExpression);
        context.SetPendingLink(previousAssertion, combinerType);
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

    /// <summary>
    /// Asserts that the value's type is assignable from the specified type.
    /// This instance method allows single type parameter usage without needing to specify the source type.
    /// Example: await Assert.That(myObject).IsAssignableFrom&lt;DerivedClass&gt;();
    /// </summary>
    public IsAssignableFromAssertion<TTarget, TValue> IsAssignableFrom<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableFrom<{typeof(TTarget).Name}>()");
        return new IsAssignableFromAssertion<TTarget, TValue>(Context);
    }

    /// <summary>
    /// Asserts that the value's type is NOT assignable from the specified type.
    /// This instance method allows single type parameter usage without needing to specify the source type.
    /// Example: await Assert.That(myObject).IsNotAssignableFrom&lt;DerivedClass&gt;();
    /// </summary>
    public IsNotAssignableFromAssertion<TTarget, TValue> IsNotAssignableFrom<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableFrom<{typeof(TTarget).Name}>()");
        return new IsNotAssignableFromAssertion<TTarget, TValue>(Context);
    }

    /// <summary>
    /// Asserts that the value is NOT of the specified type.
    /// This instance method allows single type parameter usage without needing to specify the source type.
    /// Example: await Assert.That(myObject).IsNotTypeOf<string>();
    /// </summary>
    public IsNotTypeOfAssertion<TValue, TExpected> IsNotTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsNotTypeOf<{typeof(TExpected).Name}>()");
        return new IsNotTypeOfAssertion<TValue, TExpected>(Context);
    }
}
