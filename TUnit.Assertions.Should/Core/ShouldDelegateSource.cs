using System.ComponentModel;
using System.Linq;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Should.Core;

/// <summary>
/// Should-flavored entry wrapper for delegates and async functions. Surfaces
/// <c>Throw&lt;TException&gt;</c> / <c>ThrowExactly&lt;TException&gt;</c> instance methods
/// that mirror <c>FuncAssertion</c>/<c>DelegateAssertion</c> behavior in TUnit.Assertions.
/// </summary>
public readonly struct ShouldDelegateSource<T> : IShouldSource<T>
{
    public AssertionContext<T> Context { get; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ShouldDelegateSource(AssertionContext<T> context) => Context = context;

    /// <summary>
    /// Asserts the delegate throws an exception of <typeparamref name="TException"/> or a subclass.
    /// </summary>
    public ShouldAssertion<TException> Throw<TException>() where TException : Exception
    {
        Context.ExpressionBuilder.Append($".Throw<{FormatTypeName(typeof(TException))}>()");
        var mapped = Context.MapException<TException>();
        return new ShouldAssertion<TException>(mapped, new ThrowsAssertion<TException>(mapped));
    }

    /// <summary>
    /// Asserts the delegate throws an exception of exactly <typeparamref name="TException"/> (no subclasses).
    /// </summary>
    public ShouldAssertion<TException> ThrowExactly<TException>() where TException : Exception
    {
        Context.ExpressionBuilder.Append($".ThrowExactly<{FormatTypeName(typeof(TException))}>()");
        var mapped = Context.MapException<TException>();
        return new ShouldAssertion<TException>(mapped, new ThrowsExactlyAssertion<TException>(mapped));
    }

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
    private static string FormatTypeName(System.Type t)
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
