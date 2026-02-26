#if NET9_0_OR_GREATER

namespace TUnit.Mocks.Arguments;

/// <summary>
/// Represents an argument matcher for a ref struct parameter in mock setup and verification expressions.
/// Since ref struct types cannot be used as generic type arguments for <see cref="Arg{T}"/>,
/// this type uses the <c>allows ref struct</c> anti-constraint (C# 13+) to accept them.
/// </summary>
/// <remarks>
/// Only <see cref="Any"/> matching is supported. Exact value matching and predicate matching are not
/// available for ref struct parameters because ref structs cannot be boxed or stored in closures.
///
/// Ref struct parameters are excluded from the typed <c>Callback</c>, <c>Returns</c>, and <c>Throws</c>
/// delegate overloads because lambdas cannot capture ref struct values. Use the untyped
/// <c>Action</c>/<c>Func&lt;object?[], ...&gt;</c> overloads if you need to react to all arguments.
/// </remarks>
/// <typeparam name="T">The ref struct parameter type.</typeparam>
public readonly struct RefStructArg<T> where T : allows ref struct
{
    /// <summary>Gets the argument matcher. Public for generated code access. Not intended for direct use.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public IArgumentMatcher Matcher { get; }

    /// <summary>Creates a RefStructArg with a matcher. Public for generated code access. Not intended for direct use.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public RefStructArg(IArgumentMatcher matcher)
    {
        Matcher = matcher;
    }

    /// <summary>Matches any value of the ref struct type. This is currently the only supported matcher for ref struct parameters.</summary>
    public static RefStructArg<T> Any => new(Mocks.Matchers.AnyMatcher.Instance);
}

#endif
