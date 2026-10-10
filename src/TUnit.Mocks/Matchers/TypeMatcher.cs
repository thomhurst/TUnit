using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Matchers;

/// <summary>
/// Matches arguments that are an instance of the given type (or a type derived from it).
/// Null never matches.
/// </summary>
internal sealed class TypeMatcher<T> : IArgumentMatcher<T>
{
    private readonly Type _type;

    public TypeMatcher(Type type)
    {
        var parameterType = typeof(T);

        // Reject types that can never overlap the parameter type, so a mistake fails at setup time
        // instead of silently never matching. An interface may still be implemented by a non-sealed class.
        var related = parameterType.IsAssignableFrom(type) || type.IsAssignableFrom(parameterType)
            || (type.IsInterface && !parameterType.IsSealed)
            || (parameterType.IsInterface && !type.IsSealed);

        if (!related)
        {
            // No paramName: the argument is not something the caller passed to a method they called.
            throw new ArgumentException(
                $"Arg.IsOfType<{type.Name}>() cannot be used for an argument of type {parameterType.Name}: " +
                $"{type.Name} is unrelated to {parameterType.Name}, so it could never match. " +
                $"Use a type that derives from, or is assignable to, {parameterType.Name}.");
        }

        _type = type;
    }

    public bool Matches(T? value) => value is not null && _type.IsInstanceOfType(value);

    public bool Matches(object? value) => value is not null && _type.IsInstanceOfType(value);

    public string Describe() => "Arg.IsOfType<" + _type.Name + ">";
}
