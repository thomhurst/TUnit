using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Matchers;

/// <summary>
/// Matches arguments that are an instance of the given type (or a type derived from it).
/// Null never matches.
/// </summary>
internal sealed class TypeMatcher<T> : IArgumentMatcher<T>
{
    private readonly Type _type;

    public TypeMatcher(Type type) => _type = type;

    public bool Matches(T? value) => value is not null && _type.IsInstanceOfType(value);

    public bool Matches(object? value) => value is not null && _type.IsInstanceOfType(value);

    public string Describe() => "Arg.IsOfType<" + _type.Name + ">";
}
