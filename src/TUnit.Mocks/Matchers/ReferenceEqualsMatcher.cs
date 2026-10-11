using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Matchers;

/// <summary>
/// Matches only the exact same object instance, ignoring any <c>Equals</c> override.
/// A value type has no identity, so a value-type <c>expected</c> matches an equal value instead
/// (each call boxes the argument again, so reference equality could never hold).
/// </summary>
internal sealed class ReferenceEqualsMatcher<T> : IArgumentMatcher<T>
{
    private readonly object _expected;
    private readonly T _typedExpected;
    private readonly bool _byValue;

    public ReferenceEqualsMatcher(object expected)
    {
        _expected = expected ?? throw new ArgumentNullException(nameof(expected));

        if (expected is not T typedExpected)
        {
            throw new ArgumentException(
                $"Arg.IsSameAs({MockRegistry.FormatTypeName(expected.GetType(), includeNamespace: false)} instance) can never match a parameter of type {MockRegistry.FormatTypeName(typeof(T), includeNamespace: false)}.");
        }

        _typedExpected = typedExpected;
        _byValue = expected.GetType().IsValueType;
    }

    public bool Matches(T? value) => _byValue
        ? EqualityComparer<T>.Default.Equals(_typedExpected, value!)
        : ReferenceEquals(_expected, value);

    // Same comparer as the typed overload, so setup matching and verification agree.
    public bool Matches(object? value) => _byValue
        ? value is T typed && Matches(typed)
        : ReferenceEquals(_expected, value);

    public string Describe() => _byValue
        ? "Arg.IsSameAs(" + _expected + ")"
        : "Arg.IsSameAs(" + MockRegistry.FormatTypeName(_expected.GetType(), includeNamespace: false) + " instance)";
}
