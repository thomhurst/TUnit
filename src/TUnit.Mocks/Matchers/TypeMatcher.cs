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
        // instead of silently never matching.
        if (!CanOverlap(underlyingParameterType, underlyingType))
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

    /// <summary>True when some runtime value could be an instance of both <paramref name="a"/> and <paramref name="b"/>.</summary>
    private static bool CanOverlap(Type a, Type b)
        => a.IsAssignableFrom(b) || b.IsAssignableFrom(a)
            // An interface may still be implemented by a subclass of a non-sealed class.
            || (b.IsInterface && !a.IsSealed)
            || (a.IsInterface && !b.IsSealed)
            || CanOverlapThroughArrayCovariance(a, b)
            || CanOverlapThroughArrayCovariance(b, a);

    // Generic interfaces a single-dimensional array implements for its element type.
    private static readonly Type[] ArrayGenericInterfaces =
    [
        typeof(IEnumerable<>), typeof(ICollection<>), typeof(IList<>),
        typeof(IReadOnlyCollection<>), typeof(IReadOnlyList<>),
    ];

    /// <summary>
    /// A reference-element array type is sealed, but covariance lets it hold a more derived array:
    /// an <c>object[]</c> can hold a <c>string[]</c>, which implements <c>IList&lt;string&gt;</c>. So the
    /// array overlaps such a generic interface when the interface's element type could overlap its own.
    /// Interfaces arrays never implement (such as <c>IDisposable</c>) still do not overlap.
    /// </summary>
    private static bool CanOverlapThroughArrayCovariance(Type array, Type @interface)
    {
        if (!array.IsArray || array.GetArrayRank() != 1 || !@interface.IsInterface || !@interface.IsGenericType)
        {
            return false;
        }

        var elementType = array.GetElementType()!;
        if (elementType.IsValueType || Array.IndexOf(ArrayGenericInterfaces, @interface.GetGenericTypeDefinition()) < 0)
        {
            return false;
        }

        var interfaceElementType = @interface.GetGenericArguments()[0];
        return !interfaceElementType.IsValueType && CanOverlap(elementType, interfaceElementType);
    }

    public bool Matches(T? value) => value is not null && _type.IsInstanceOfType(value);

    public bool Matches(object? value) => value is not null && _type.IsInstanceOfType(value);

    public string Describe() => "Arg.IsOfType<" + MockRegistry.FormatTypeName(_type, includeNamespace: false) + ">";
}
