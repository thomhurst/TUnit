using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Should.Core;

/// <summary>
/// Should-flavored entry wrapper for delegates and async functions. Surfaces
/// <c>Throw&lt;TException&gt;</c> / <c>ThrowExactly&lt;TException&gt;</c> instance methods
/// that mirror <c>FuncAssertion</c>/<c>DelegateAssertion</c> behavior in TUnit.Assertions.
/// </summary>
public sealed class ShouldDelegateSource<T> : IShouldSource<T>
{
    public AssertionContext<T> Context { get; }

    public ShouldDelegateSource(AssertionContext<T> context) => Context = context;

    /// <summary>
    /// Asserts the delegate throws an exception of <typeparamref name="TException"/> or a subclass.
    /// </summary>
    public ShouldAssertion<TException> Throw<TException>() where TException : Exception
    {
        Context.ExpressionBuilder.Append($".Throw<{typeof(TException).Name}>()");
        var mapped = Context.MapException<TException>();
        return new ShouldAssertion<TException>(mapped, new ThrowsAssertion<TException>(mapped));
    }

    /// <summary>
    /// Asserts the delegate throws an exception of exactly <typeparamref name="TException"/> (no subclasses).
    /// </summary>
    public ShouldAssertion<TException> ThrowExactly<TException>() where TException : Exception
    {
        Context.ExpressionBuilder.Append($".ThrowExactly<{typeof(TException).Name}>()");
        var mapped = Context.MapException<TException>();
        return new ShouldAssertion<TException>(mapped, new ThrowsExactlyAssertion<TException>(mapped));
    }
}
