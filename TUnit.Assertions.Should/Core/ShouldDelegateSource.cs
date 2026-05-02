using System.ComponentModel;
using System.Linq;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Should.Core;

internal static class DelegateExceptionTypeFormatter
{
    /// <summary>
    /// Renders a type's display name for the assertion's expression-builder string. Strips the
    /// backtick-arity suffix and recurses into generic arguments so that
    /// <c>typeof(MyException&lt;int&gt;)</c> appears as <c>MyException&lt;Int32&gt;</c> in
    /// failure messages rather than the raw <c>MyException`1</c> that <see cref="System.Type.Name"/>
    /// returns. Note: this runs at runtime (no Roslyn) so primitive aliases come through as their
    /// CLR names (<c>Int32</c>, not <c>int</c>); the source-generator's emit path uses Roslyn's
    /// display format and produces <c>int</c>. The asymmetry is acceptable for failure messages
    /// — exception types are rarely generic and almost never primitive — but is worth noting.
    /// </summary>
    internal static string FormatTypeName(System.Type t)
    {
        if (!t.IsGenericType)
        {
            return t.Name;
        }

        var name = t.Name;
        var tickIndex = name.IndexOf('`');
        if (tickIndex > 0)
        {
            name = name.Substring(0, tickIndex);
        }

        return $"{name}<{string.Join(", ", t.GenericTypeArguments.Select(FormatTypeName))}>";
    }
}

/// <summary>
/// Should-flavored entry wrapper for delegates and async functions. Surfaces
/// <c>Throw&lt;TException&gt;</c> / <c>ThrowExactly&lt;TException&gt;</c> instance methods
/// that mirror <c>FuncAssertion</c>/<c>DelegateAssertion</c> behavior in TUnit.Assertions.
/// </summary>
public readonly struct ShouldDelegateSource<T> : IShouldSource<T>
{
    private readonly string? _becauseMessage;

    public AssertionContext<T> Context { get; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ShouldDelegateSource(AssertionContext<T> context) : this(context, becauseMessage: null)
    {
    }

    private ShouldDelegateSource(AssertionContext<T> context, string? becauseMessage)
    {
        Context = context;
        _becauseMessage = becauseMessage;
    }

    /// <summary>
    /// Attaches a human-readable reason to the next assertion in the chain. Returns a NEW struct —
    /// because <see cref="ShouldDelegateSource{T}"/> is a <c>readonly struct</c>, the result MUST
    /// be consumed inline (e.g. <c>source.Because("...").Throw&lt;E&gt;()</c>). Assigning it to a
    /// variable and continuing on the original copy silently drops the message.
    /// </summary>
    public ShouldDelegateSource<T> Because(string message)
        => new(Context, message.Trim());

    string? IShouldSource<T>.ConsumeBecauseMessage()
        => _becauseMessage;

    /// <summary>
    /// Asserts the delegate throws an exception of <typeparamref name="TException"/> or a subclass.
    /// </summary>
    public ShouldAssertion<TException> Throw<TException>() where TException : Exception
    {
        Context.ExpressionBuilder.Append($".Throw<{DelegateExceptionTypeFormatter.FormatTypeName(typeof(TException))}>()");
        var mapped = Context.MapException<TException>();
        var inner = new ThrowsAssertion<TException>(mapped);
        ApplyBecause(inner);
        return new ShouldAssertion<TException>(mapped, inner);
    }

    /// <summary>
    /// Asserts the delegate throws an exception of exactly <typeparamref name="TException"/> (no subclasses).
    /// </summary>
    public ShouldAssertion<TException> ThrowExactly<TException>() where TException : Exception
    {
        Context.ExpressionBuilder.Append($".ThrowExactly<{DelegateExceptionTypeFormatter.FormatTypeName(typeof(TException))}>()");
        var mapped = Context.MapException<TException>();
        var inner = new ThrowsExactlyAssertion<TException>(mapped);
        ApplyBecause(inner);
        return new ShouldAssertion<TException>(mapped, inner);
    }

    private void ApplyBecause<TAssertionValue>(Assertion<TAssertionValue> assertion)
    {
        if (_becauseMessage is not null)
        {
            assertion.Because(_becauseMessage);
        }
    }
}
