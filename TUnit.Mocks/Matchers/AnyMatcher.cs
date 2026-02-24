using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Matchers;

/// <summary>
/// An argument matcher that matches any value of the specified type, including null.
/// </summary>
internal sealed class AnyMatcher<T> : IArgumentMatcher<T>
{
    public bool Matches(T? value) => true;

    public bool Matches(object? value) => true;

    public string Describe() => "Any<" + typeof(T).Name + ">";
}
