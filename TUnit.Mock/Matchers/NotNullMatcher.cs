using TUnit.Mock.Arguments;

namespace TUnit.Mock.Matchers;

/// <summary>
/// An argument matcher that matches only when the value is not null.
/// </summary>
internal sealed class NotNullMatcher<T> : IArgumentMatcher<T>
{
    public bool Matches(T? value) => value is not null;

    public bool Matches(object? value) => value is not null;

    public string Describe() => "IsNotNull<" + typeof(T).Name + ">";
}
