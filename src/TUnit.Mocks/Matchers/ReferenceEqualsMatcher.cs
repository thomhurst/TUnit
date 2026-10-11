using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Matchers;

/// <summary>
/// Matches only the exact same object instance, ignoring any <c>Equals</c> override.
/// </summary>
internal sealed class ReferenceEqualsMatcher<T> : IArgumentMatcher<T>
{
    private readonly object _expected;

    public ReferenceEqualsMatcher(object expected)
    {
        _expected = expected ?? throw new ArgumentNullException(nameof(expected));

        if (typeof(T).IsValueType)
        {
            throw new ArgumentException(
                $"Arg.IsSameAs(...) compares references and cannot be used for a parameter of value type {MockRegistry.FormatTypeName(typeof(T), includeNamespace: false)}.");
        }

        if (expected is not T)
        {
            throw new ArgumentException(
                $"Arg.IsSameAs({MockRegistry.FormatTypeName(expected.GetType(), includeNamespace: false)} instance) can never match a parameter of type {MockRegistry.FormatTypeName(typeof(T), includeNamespace: false)}.");
        }
    }

    public bool Matches(T? value) => ReferenceEquals(_expected, value);

    public bool Matches(object? value) => ReferenceEquals(_expected, value);

    public string Describe() => "Arg.IsSameAs(" + MockRegistry.FormatTypeName(_expected.GetType(), includeNamespace: false) + " instance)";
}
