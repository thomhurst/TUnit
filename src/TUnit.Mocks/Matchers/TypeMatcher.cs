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

        // A non-null Nullable<X> argument arrives boxed as X, so compare against the underlying types:
        // reflection doesn't report X's interfaces on Nullable<X>, and Nullable<X> itself is sealed.
        var underlyingParameterType = Nullable.GetUnderlyingType(parameterType) ?? parameterType;
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        // Reject types that can never overlap the parameter type, so a mistake fails at setup time
        // instead of silently never matching. An interface may still be implemented by a non-sealed class.
        var related = underlyingParameterType.IsAssignableFrom(underlyingType) || underlyingType.IsAssignableFrom(underlyingParameterType)
            || (underlyingType.IsInterface && CanHoldSubtypes(underlyingParameterType))
            || (underlyingParameterType.IsInterface && CanHoldSubtypes(underlyingType));

        if (!related)
        {
            var typeName = MockRegistry.FormatTypeName(type, includeNamespace: false);
            var parameterTypeName = MockRegistry.FormatTypeName(parameterType, includeNamespace: false);
            // No paramName: the argument is not something the caller passed to a method they called.
            throw new ArgumentException(
                $"Arg.IsOfType<{typeName}>() cannot be used for an argument of type {parameterTypeName}: " +
                $"{typeName} is unrelated to {parameterTypeName}, so it could never match. " +
                $"Use a type that can overlap with {parameterTypeName}.");
        }

        _type = type;
    }

    // A reference-element array type is sealed, but array covariance lets it hold e.g. a string[]
    // for object[], which implements interfaces (IList<string>) that object[] itself does not.
    private static bool CanHoldSubtypes(Type type)
        => !type.IsSealed || (type.IsArray && !type.GetElementType()!.IsValueType);

    public bool Matches(T? value) => value is not null && _type.IsInstanceOfType(value);

    public bool Matches(object? value) => value is not null && _type.IsInstanceOfType(value);

    public string Describe() => "Arg.IsOfType<" + MockRegistry.FormatTypeName(_type, includeNamespace: false) + ">";
}
