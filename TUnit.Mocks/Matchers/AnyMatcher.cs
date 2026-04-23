using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Matchers;

/// <summary>
/// A non-generic argument matcher that matches any value including null.
/// Used for ref struct parameter positions where the generic AnyMatcher&lt;T&gt; cannot be used.
/// </summary>
internal sealed class AnyMatcher : IArgumentMatcher
{
    public static AnyMatcher Instance { get; } = new();

    public bool Matches(object? value) => true;

    public string Describe() => "Any";
}

/// <summary>
/// An argument matcher that matches any value of the specified type, including null.
/// </summary>
internal sealed class AnyMatcher<T> : IArgumentMatcher<T>
{
    /// <summary>
    /// Cached singleton — <see cref="AnyMatcher{T}"/> is stateless, so a single instance per closed
    /// generic type avoids a per-call allocation on the common <see cref="Arg.Any{T}"/> path.
    /// </summary>
    public static readonly AnyMatcher<T> Instance = new();

    public bool Matches(T? value) => true;

    public bool Matches(object? value) => true;

    public string Describe() => "Any<" + typeof(T).Name + ">";
}
