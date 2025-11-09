using System.Text;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for synchronous delegates (Action).
/// This is the entry point for: Assert.That(() => SomeMethod())
/// Used primarily for exception checking.
/// Implements IDelegateAssertionSource to enable Throws() extension methods.
/// Does not inherit from Assertion to prevent premature awaiting.
/// </summary>
public class DelegateAssertion : IAssertionSource<object?>, IDelegateAssertionSource<object?>
{
    public AssertionContext<object?> Context { get; }
    internal Action Action { get; }

    public DelegateAssertion(Action action, string? expression)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        var expressionBuilder = new StringBuilder();
        expressionBuilder.Append($"Assert.That({expression ?? "?"})");
        var evaluationContext = new EvaluationContext<object?>(() =>
        {
            try
            {
                action();
                return Task.FromResult<(object?, Exception?)>((null, null));
            }
            catch (Exception ex)
            {
                return Task.FromResult<(object?, Exception?)>((null, ex));
            }
        });
        Context = new AssertionContext<object?>(evaluationContext, expressionBuilder);
    }

    /// <summary>
    /// Asserts that the value is of the specified type and returns an assertion on the casted value.
    /// Example: await Assert.That(() => SomeMethod()).IsTypeOf<string>();
    /// </summary>
    public TypeOfAssertion<object?, TExpected> IsTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<object?, TExpected>(Context);
    }

    public IsAssignableToAssertion<TTarget, object?> IsAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableTo<{typeof(TTarget).Name}>()");
        return new IsAssignableToAssertion<TTarget, object?>(Context);
    }

    public IsNotAssignableToAssertion<TTarget, object?> IsNotAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableTo<{typeof(TTarget).Name}>()");
        return new IsNotAssignableToAssertion<TTarget, object?>(Context);
    }

    /// <summary>
    /// Asserts that the value is NOT of the specified type.
    /// Example: await Assert.That(() => SomeMethod()).IsNotTypeOf<int>();
    /// </summary>
    public IsNotTypeOfAssertion<object?, TExpected> IsNotTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsNotTypeOf<{typeof(TExpected).Name}>()");
        return new IsNotTypeOfAssertion<object?, TExpected>(Context);
    }

    /// <summary>
    /// Asserts that the delegate throws the specified exception type (or subclass).
    /// Instance method to avoid C# type inference issues with extension methods.
    /// Example: await Assert.That(() => ThrowingMethod()).Throws&lt;InvalidOperationException&gt;();
    /// </summary>
    public ThrowsAssertion<TException> Throws<TException>() where TException : Exception
    {
        Context.ExpressionBuilder.Append($".Throws<{typeof(TException).Name}>()");
        var mappedContext = Context.MapException<TException>();
        return new ThrowsAssertion<TException>(mappedContext!);
    }

    /// <summary>
    /// Asserts that the delegate throws exactly the specified exception type (not subclasses).
    /// Instance method to avoid C# type inference issues with extension methods.
    /// Example: await Assert.That(() => ThrowingMethod()).ThrowsExactly&lt;InvalidOperationException&gt;();
    /// </summary>
    public ThrowsExactlyAssertion<TException> ThrowsExactly<TException>() where TException : Exception
    {
        Context.ExpressionBuilder.Append($".ThrowsExactly<{typeof(TException).Name}>()");
        var mappedContext = Context.MapException<TException>();
        return new ThrowsExactlyAssertion<TException>(mappedContext!);
    }
}
