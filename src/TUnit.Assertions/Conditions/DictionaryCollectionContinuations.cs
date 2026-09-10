using System.Runtime.CompilerServices;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Dictionary-typed assertion that delegates its check to an inner collection assertion while
/// preserving dictionary-specific chaining (ContainsKey, Value, ...).
/// The inner assertion is built on a detached context so its construction does not consume the
/// And/Or pending link that must stay wired to this (dictionary-typed) assertion.
/// </summary>
internal sealed class DictionaryDelegatingAssertion<TDictionary, TKey, TValue>
    : Sources.DictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly Assertion<TDictionary> _inner;

    internal DictionaryDelegatingAssertion(AssertionContext<TDictionary> context, Assertion<TDictionary> inner)
        : base(context)
    {
        _inner = inner;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TDictionary> metadata)
        => _inner.InternalCheckAsync(metadata);

    protected override string GetExpectation() => _inner.InternalGetExpectation();
}

/// <summary>
/// Mutable-dictionary twin of <see cref="DictionaryDelegatingAssertion{TDictionary,TKey,TValue}"/>.
/// </summary>
internal sealed class MutableDictionaryDelegatingAssertion<TDictionary, TKey, TValue>
    : Sources.MutableDictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly Assertion<TDictionary> _inner;

    internal MutableDictionaryDelegatingAssertion(AssertionContext<TDictionary> context, Assertion<TDictionary> inner)
        : base(context)
    {
        _inner = inner;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TDictionary> metadata)
        => _inner.InternalCheckAsync(metadata);

    protected override string GetExpectation() => _inner.InternalGetExpectation();
}

/// <summary>
/// Count source for read-only dictionaries that preserves dictionary-specific chaining.
/// Mirrors <see cref="CollectionCountSource{TCollection,TItem}"/> but returns dictionary-typed
/// assertions so chains like <c>Count().IsEqualTo(2).And.ContainsKey("k")</c> keep working.
/// NOTE: keep the method set in sync with <see cref="MutableDictionaryCountSource{TDictionary,TKey,TValue}"/>
/// (separate classes are required by the IReadOnlyDictionary vs IDictionary constraint).
/// </summary>
public sealed class DictionaryCountSource<TDictionary, TKey, TValue>
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly AssertionContext<TDictionary> _context;

    internal DictionaryCountSource(AssertionContext<TDictionary> context) => _context = context;

    private Sources.DictionaryAssertionBase<TDictionary, TKey, TValue> Compare(
        int expected, CountComparison comparison, string expressionFragment)
    {
        _context.ExpressionBuilder.Append(expressionFragment);
        var inner = new CollectionCountEqualsAssertion<TDictionary, KeyValuePair<TKey, TValue>>(
            _context.CreateDetached(), null, expected, comparison);
        return new DictionaryDelegatingAssertion<TDictionary, TKey, TValue>(_context, inner);
    }

    public Sources.DictionaryAssertionBase<TDictionary, TKey, TValue> IsEqualTo(
        int expected, [CallerArgumentExpression(nameof(expected))] string? expression = null)
        => Compare(expected, CountComparison.Equal, $".IsEqualTo({expression})");

    public Sources.DictionaryAssertionBase<TDictionary, TKey, TValue> IsNotEqualTo(
        int expected, [CallerArgumentExpression(nameof(expected))] string? expression = null)
        => Compare(expected, CountComparison.NotEqual, $".IsNotEqualTo({expression})");

    public Sources.DictionaryAssertionBase<TDictionary, TKey, TValue> IsGreaterThan(
        int expected, [CallerArgumentExpression(nameof(expected))] string? expression = null)
        => Compare(expected, CountComparison.GreaterThan, $".IsGreaterThan({expression})");

    public Sources.DictionaryAssertionBase<TDictionary, TKey, TValue> IsGreaterThanOrEqualTo(
        int expected, [CallerArgumentExpression(nameof(expected))] string? expression = null)
        => Compare(expected, CountComparison.GreaterThanOrEqual, $".IsGreaterThanOrEqualTo({expression})");

    public Sources.DictionaryAssertionBase<TDictionary, TKey, TValue> IsLessThan(
        int expected, [CallerArgumentExpression(nameof(expected))] string? expression = null)
        => Compare(expected, CountComparison.LessThan, $".IsLessThan({expression})");

    public Sources.DictionaryAssertionBase<TDictionary, TKey, TValue> IsLessThanOrEqualTo(
        int expected, [CallerArgumentExpression(nameof(expected))] string? expression = null)
        => Compare(expected, CountComparison.LessThanOrEqual, $".IsLessThanOrEqualTo({expression})");

    public Sources.DictionaryAssertionBase<TDictionary, TKey, TValue> IsZero()
        => Compare(0, CountComparison.Equal, ".IsZero()");

    public Sources.DictionaryAssertionBase<TDictionary, TKey, TValue> IsPositive()
        => Compare(0, CountComparison.GreaterThan, ".IsPositive()");
}

/// <summary>
/// Mutable-dictionary twin of <see cref="DictionaryCountSource{TDictionary,TKey,TValue}"/>.
/// NOTE: keep the method set in sync with <see cref="DictionaryCountSource{TDictionary,TKey,TValue}"/>.
/// </summary>
public sealed class MutableDictionaryCountSource<TDictionary, TKey, TValue>
    where TDictionary : IDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly AssertionContext<TDictionary> _context;

    internal MutableDictionaryCountSource(AssertionContext<TDictionary> context) => _context = context;

    private Sources.MutableDictionaryAssertionBase<TDictionary, TKey, TValue> Compare(
        int expected, CountComparison comparison, string expressionFragment)
    {
        _context.ExpressionBuilder.Append(expressionFragment);
        var inner = new CollectionCountEqualsAssertion<TDictionary, KeyValuePair<TKey, TValue>>(
            _context.CreateDetached(), null, expected, comparison);
        return new MutableDictionaryDelegatingAssertion<TDictionary, TKey, TValue>(_context, inner);
    }

    public Sources.MutableDictionaryAssertionBase<TDictionary, TKey, TValue> IsEqualTo(
        int expected, [CallerArgumentExpression(nameof(expected))] string? expression = null)
        => Compare(expected, CountComparison.Equal, $".IsEqualTo({expression})");

    public Sources.MutableDictionaryAssertionBase<TDictionary, TKey, TValue> IsNotEqualTo(
        int expected, [CallerArgumentExpression(nameof(expected))] string? expression = null)
        => Compare(expected, CountComparison.NotEqual, $".IsNotEqualTo({expression})");

    public Sources.MutableDictionaryAssertionBase<TDictionary, TKey, TValue> IsGreaterThan(
        int expected, [CallerArgumentExpression(nameof(expected))] string? expression = null)
        => Compare(expected, CountComparison.GreaterThan, $".IsGreaterThan({expression})");

    public Sources.MutableDictionaryAssertionBase<TDictionary, TKey, TValue> IsGreaterThanOrEqualTo(
        int expected, [CallerArgumentExpression(nameof(expected))] string? expression = null)
        => Compare(expected, CountComparison.GreaterThanOrEqual, $".IsGreaterThanOrEqualTo({expression})");

    public Sources.MutableDictionaryAssertionBase<TDictionary, TKey, TValue> IsLessThan(
        int expected, [CallerArgumentExpression(nameof(expected))] string? expression = null)
        => Compare(expected, CountComparison.LessThan, $".IsLessThan({expression})");

    public Sources.MutableDictionaryAssertionBase<TDictionary, TKey, TValue> IsLessThanOrEqualTo(
        int expected, [CallerArgumentExpression(nameof(expected))] string? expression = null)
        => Compare(expected, CountComparison.LessThanOrEqual, $".IsLessThanOrEqualTo({expression})");

    public Sources.MutableDictionaryAssertionBase<TDictionary, TKey, TValue> IsZero()
        => Compare(0, CountComparison.Equal, ".IsZero()");

    public Sources.MutableDictionaryAssertionBase<TDictionary, TKey, TValue> IsPositive()
        => Compare(0, CountComparison.GreaterThan, ".IsPositive()");
}
