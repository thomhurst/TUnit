using System.Text.RegularExpressions;
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

    /// <summary>Matches a string against a regular expression pattern.</summary>
    public static Arg<string> Matches(string pattern) => new(new RegexMatcher(pattern));

    /// <summary>Matches a string against a compiled <see cref="Regex"/>.</summary>
    public static Arg<string> Matches(Regex regex) => new(new RegexMatcher(regex));

    /// <summary>Matches using a user-defined custom matcher.</summary>
    public static Arg<T> Matches<T>(IArgumentMatcher<T> matcher) => new(matcher);

    /// <summary>Matches a collection containing the specified item.</summary>
    public static Arg<TCollection> Contains<TCollection, TElement>(TElement item)
        where TCollection : IEnumerable<TElement>
        => new(new ContainsMatcher<TElement>(item));

    /// <summary>Matches a collection with the specified number of elements.</summary>
    public static Arg<T> HasCount<T>(int count) => new(new CountMatcher(count));

    /// <summary>Matches an empty collection.</summary>
    public static Arg<T> IsEmpty<T>() => new(new EmptyMatcher());

    /// <summary>Matches a collection with element-by-element equality.</summary>
    public static Arg<TCollection> SequenceEquals<TCollection, TElement>(IEnumerable<TElement> expected)
        where TCollection : IEnumerable<TElement>
        => new(new SequenceEqualsMatcher<TElement>(expected));
}
