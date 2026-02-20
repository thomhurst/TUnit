using TUnit.Mock.Matchers;

namespace TUnit.Mock.Arguments;

/// <summary>
/// Provides static factory methods for creating argument matchers
/// used in mock setup and verification expressions.
/// </summary>
public static class Arg
{
    /// <summary>Matches any value of the specified type, including null.</summary>
    public static Arg<T> Any<T>() => new(new AnyMatcher<T>());

    /// <summary>Matches using exact equality.</summary>
    public static Arg<T> Is<T>(T value) => new(new ExactMatcher<T>(value));

    /// <summary>Matches when the predicate returns true for the actual argument.</summary>
    public static Arg<T> Is<T>(Func<T?, bool> predicate) => new(new PredicateMatcher<T>(predicate));

    /// <summary>Matches only when the argument is null.</summary>
    public static Arg<T> IsNull<T>() where T : class => new(new NullMatcher<T>());

    /// <summary>Matches only when the argument is not null.</summary>
    public static Arg<T> IsNotNull<T>() where T : class => new(new NotNullMatcher<T>());

    /// <summary>Matches any value and captures it into the supplied <see cref="ArgCapture{T}"/>.</summary>
    public static Arg<T> Capture<T>(ArgCapture<T> capture) => new(new CaptureMatcher<T>(capture));
}
