using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Matchers;

/// <summary>
/// An argument matcher that matches only when the value is not null.
/// </summary>
internal sealed class NotNullMatcher<T> : IArgumentMatcher<T>
{
    public bool Matches(T? value) => value is not null;

    public bool Matches(object? value) => value is not null;

    public string Describe() => "IsNotNull<" + typeof(T).Name + ">";
}
