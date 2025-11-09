using System.Text;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for synchronous functions.
/// This is the entry point for: Assert.That(() => GetValue())
/// Implements IDelegateAssertionSource to enable Throws() extension methods.
/// Does not inherit from Assertion to prevent premature awaiting.
/// </summary>
public class FuncAssertion<TValue> : IAssertionSource<TValue>, IDelegateAssertionSource<TValue>
{
    public AssertionContext<TValue> Context { get; }

    public FuncAssertion(Func<TValue?> func, string? expression)
    {
        var expressionBuilder = new StringBuilder();
        expressionBuilder.Append($"Assert.That({expression ?? "?"})");
        var evaluationContext = new EvaluationContext<TValue>(() =>
        {
            try
            {
                var result = func();
                return Task.FromResult<(TValue?, Exception?)>((result, null));
            }
            catch (Exception ex)
            {
                return Task.FromResult<(TValue?, Exception?)>((default(TValue), ex));
            }
        });
        Context = new AssertionContext<TValue>(evaluationContext, expressionBuilder);
    }

    /// <summary>
    /// Asserts that the value is of the specified type and returns an assertion on the casted value.
    /// Example: await Assert.That(() => GetValue()).IsTypeOf<string>();
    /// </summary>
    public TypeOfAssertion<TValue, TExpected> IsTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<TValue, TExpected>(Context);
    }

    public IsAssignableToAssertion<TTarget, TValue> IsAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableTo<{typeof(TTarget).Name}>()");
        return new IsAssignableToAssertion<TTarget, TValue>(Context);
    }

    public IsNotAssignableToAssertion<TTarget, TValue> IsNotAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableTo<{typeof(TTarget).Name}>()");
        return new IsNotAssignableToAssertion<TTarget, TValue>(Context);
    }

    /// <summary>
    /// Asserts that the value is NOT of the specified type.
    /// Example: await Assert.That(() => GetValue()).IsNotTypeOf<int>();
    /// </summary>
    public IsNotTypeOfAssertion<TValue, TExpected> IsNotTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsNotTypeOf<{typeof(TExpected).Name}>()");
        return new IsNotTypeOfAssertion<TValue, TExpected>(Context);
    }

    /// <summary>
    /// Asserts that the function throws the specified exception type (or subclass).
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
    /// Asserts that the function throws exactly the specified exception type (not subclasses).
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
