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
    public bool Matches(T? value) => true;

    public bool Matches(object? value) => true;

    public string Describe() => "Any<" + typeof(T).Name + ">";
}
