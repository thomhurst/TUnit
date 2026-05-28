using System.Collections;
using TUnit.Assertions.Abstractions;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a collection is null, preserving collection type information.
/// Extends CollectionAssertionBase to ensure .And and .Or return collection-specific continuations.
/// </summary>
internal class CollectionNullAssertion<TCollection, TItem> : CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    public CollectionNullAssertion(AssertionContext<TCollection> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
        => NullCheck.Check(metadata, expectNull: true);

    protected override string GetExpectation() => NullCheck.Expectation(expectNull: true);
}

/// <summary>
/// Asserts that a collection is not null, preserving collection type information.
/// Extends CollectionAssertionBase to ensure .And and .Or return collection-specific continuations.
/// </summary>
public class CollectionNotNullAssertion<TCollection, TItem> : CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    public CollectionNotNullAssertion(AssertionContext<TCollection> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
        => NullCheck.Check(metadata, expectNull: false);

    protected override string GetExpectation() => NullCheck.Expectation(expectNull: false);
}

internal class ListNullAssertion<TList, TItem>(AssertionContext<TList> context, bool expectNull)
    : ListAssertionBase<TList, TItem>(context)
    where TList : IList<TItem>
{
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TList> metadata)
        => NullCheck.Check(metadata, expectNull);

    protected override string GetExpectation() => NullCheck.Expectation(expectNull);
}

internal class ReadOnlyListNullAssertion<TList, TItem>(AssertionContext<TList> context, bool expectNull)
    : ReadOnlyListAssertionBase<TList, TItem>(context)
    where TList : IReadOnlyList<TItem>
{
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TList> metadata)
        => NullCheck.Check(metadata, expectNull);

    protected override string GetExpectation() => NullCheck.Expectation(expectNull);
}

internal class DictionaryNullAssertion<TDictionary, TKey, TValue>(AssertionContext<TDictionary> context, bool expectNull)
    : DictionaryAssertionBase<TDictionary, TKey, TValue>(context)
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TDictionary> metadata)
        => NullCheck.Check(metadata, expectNull);

    protected override string GetExpectation() => NullCheck.Expectation(expectNull);
}

internal class MutableDictionaryNullAssertion<TDictionary, TKey, TValue>(AssertionContext<TDictionary> context, bool expectNull)
    : MutableDictionaryAssertionBase<TDictionary, TKey, TValue>(context)
    where TDictionary : IDictionary<TKey, TValue>
    where TKey : notnull
{
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TDictionary> metadata)
        => NullCheck.Check(metadata, expectNull);

    protected override string GetExpectation() => NullCheck.Expectation(expectNull);
}

internal class SetNullAssertion<TSet, TItem>(
    AssertionContext<TSet> context,
    Func<TSet, ISetAdapter<TItem>> adapterFactory,
    bool expectNull)
    : SetAssertionBase<TSet, TItem>(context)
    where TSet : IEnumerable<TItem>
{
    // adapterFactory is never invoked on the null-check path (the set is never materialized),
    // but SetAssertionBase requires CreateSetAdapter, so it must be supplied.
    protected override ISetAdapter<TItem> CreateSetAdapter(TSet value) => adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TSet> metadata)
        => NullCheck.Check(metadata, expectNull);

    protected override string GetExpectation() => NullCheck.Expectation(expectNull);
}

internal static class NullCheck
{
    public static Task<AssertionResult> Check<TValue>(EvaluationMetadata<TValue> metadata, bool expectNull)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}", metadata.Exception));
        }

        if (expectNull ? metadata.Value is null : metadata.Value is not null)
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed(expectNull ? "value is not null" : "value is null"));
    }

    public static string Expectation(bool expectNull) => expectNull ? "to be null" : "to not be null";
}
