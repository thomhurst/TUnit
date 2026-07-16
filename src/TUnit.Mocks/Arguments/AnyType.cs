namespace TUnit.Mocks.Arguments;

/// <summary>
/// Wildcard marker for a generic method's reference-type argument. Use it as the type argument of a
/// mock setup or verification on a generic method to match calls regardless of the concrete type
/// argument supplied at the call site:
/// <code>
/// mock.Get&lt;AnyType&gt;(Arg.Any&lt;int&gt;()).Returns(value); // matches Get&lt;Customer&gt;, Get&lt;Order&gt;, ...
/// </code>
/// Satisfies a <c>where T : class</c>, <c>where T : class, new()</c>, or unconstrained type parameter
/// (it has a public parameterless constructor). For a <c>where T : struct</c> parameter use
/// <see cref="AnyValueType"/>. Type parameters constrained to a specific base class or interface
/// support exact-type matching only.
/// </summary>
public sealed class AnyType
{
}

/// <summary>
/// Wildcard marker for a generic method's value-type argument. The struct counterpart of
/// <see cref="AnyType"/>, for matching a <c>where T : struct</c> type parameter regardless of the
/// concrete value type supplied at the call site.
/// A type parameter with constraints beyond <c>struct</c> (e.g. <c>where T : struct, IComparable</c>)
/// supports exact-type matching only, since <see cref="AnyValueType"/> does not implement any
/// interfaces and cannot be supplied as its type argument.
/// </summary>
public struct AnyValueType
{
}
