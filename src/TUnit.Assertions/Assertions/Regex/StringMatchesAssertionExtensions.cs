using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TUnit.Assertions.Assertions.Regex;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Extension methods for regex match assertions to access capture groups and individual matches.
/// </summary>
public static class RegexAssertionExtensions
{
    // ===============================================================
    // .Matches() - Maps string → RegexMatchCollection
    // ===============================================================

    /// <summary>
    /// Asserts that a string matches a regular expression pattern.
    /// Returns a collection of all matches for further assertions.
    /// </summary>
    public static StringMatchesAssertion Matches(
        this IAssertionSource<string> source,
        string pattern,
        [CallerArgumentExpression(nameof(pattern))] string? patternExpression = null)
    {
        source.Context.ExpressionBuilder.Append(".Matches(" + (patternExpression ?? "?") + ")");
        return new StringMatchesAssertion(source.Context, pattern);
    }

    /// <summary>
    /// Asserts that a string matches a regular expression.
    /// Returns a collection of all matches for further assertions.
    /// </summary>
    public static StringMatchesAssertion Matches(
        this IAssertionSource<string> source,
        System.Text.RegularExpressions.Regex regex,
        [CallerArgumentExpression(nameof(regex))] string? regexExpression = null)
    {
        source.Context.ExpressionBuilder.Append(".Matches(" + (regexExpression ?? "regex") + ")");
        return new StringMatchesAssertion(source.Context, regex);
    }

    // ===============================================================
    // .Match(index) - Maps RegexMatchCollection → RegexMatch
    // ===============================================================

    /// <summary>
    /// Gets a specific match by index from the match collection.
    /// Requires .And before .Match() for better readability.
    /// Mirrors .NET's MatchCollection[index] indexer.
    /// </summary>
    public static MatchIndexAssertion Match(
        this AndContinuation<RegexMatchCollection> continuation,
        int index)
    {
        return new MatchIndexAssertion(continuation.Context, index);
    }

    /// <summary>
    /// Asserts on a specific match by index from the match collection using a lambda.
    /// Requires .And before .Match() for better readability.
    /// </summary>
    public static MatchAssertion Match(
        this AndContinuation<RegexMatchCollection> continuation,
        int index,
        Func<ValueAssertion<RegexMatch>, Assertion<RegexMatch>?> assertion)
    {
        return new MatchAssertion(continuation.Context, index, assertion);
    }

    // ===============================================================
    // .Group() - Operates on first match in collection
    // ===============================================================

    /// <summary>
    /// Asserts on a named capture group from the first regex match.
    /// Requires .And before .Group() for better readability.
    /// </summary>
    public static GroupAssertion Group(
        this AndContinuation<RegexMatchCollection> continuation,
        string groupName,
        Func<ValueAssertion<string>, Assertion<string>?> assertion)
    {
        return new GroupAssertion(continuation.Context, groupName, assertion, true);
    }

    /// <summary>
    /// Asserts on an indexed capture group from the first regex match.
    /// Requires .And before .Group() for better readability.
    /// </summary>
    public static GroupAssertion Group(
        this AndContinuation<RegexMatchCollection> continuation,
        int groupIndex,
        Func<ValueAssertion<string>, Assertion<string>?> assertion)
    {
        return new GroupAssertion(continuation.Context, groupIndex, assertion, true);
    }

    // ===============================================================
    // .Group() - Operates on specific match (after .Match(index))
    // ===============================================================

    /// <summary>
    /// Asserts on a named capture group from a specific regex match.
    /// Can be used inside lambda expressions or after .And for chaining.
    /// </summary>
    public static MatchGroupAssertion Group(
        this ValueAssertion<RegexMatch> source,
        string groupName,
        Func<ValueAssertion<string>, Assertion<string>?> assertion)
    {
        return new MatchGroupAssertion(source.Context, groupName, assertion);
    }

    /// <summary>
    /// Asserts on an indexed capture group from a specific regex match.
    /// Can be used inside lambda expressions or after .And for chaining.
    /// </summary>
    public static MatchGroupAssertion Group(
        this ValueAssertion<RegexMatch> source,
        int groupIndex,
        Func<ValueAssertion<string>, Assertion<string>?> assertion)
    {
        return new MatchGroupAssertion(source.Context, groupIndex, assertion);
    }

    /// <summary>
    /// Asserts on a named capture group from a specific regex match.
    /// Requires .And before .Group() for better readability.
    /// </summary>
    public static MatchGroupAssertion Group(
        this AndContinuation<RegexMatch> continuation,
        string groupName,
        Func<ValueAssertion<string>, Assertion<string>?> assertion)
    {
        return new MatchGroupAssertion(continuation.Context, groupName, assertion);
    }

    /// <summary>
    /// Asserts on an indexed capture group from a specific regex match.
    /// Requires .And before .Group() for better readability.
    /// </summary>
    public static MatchGroupAssertion Group(
        this AndContinuation<RegexMatch> continuation,
        int groupIndex,
        Func<ValueAssertion<string>, Assertion<string>?> assertion)
    {
        return new MatchGroupAssertion(continuation.Context, groupIndex, assertion);
    }
}

