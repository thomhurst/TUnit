using TUnit.Mock.Arguments;

namespace TUnit.Mock.Matchers;

/// <summary>
/// An argument matcher that matches only when the value is null.
/// </summary>
internal sealed class NullMatcher<T> : IArgumentMatcher<T>
{
    public bool Matches(T? value) => value is null;

    public bool Matches(object? value) => value is null;

    public string Describe() => "IsNull<" + typeof(T).Name + ">";
}
