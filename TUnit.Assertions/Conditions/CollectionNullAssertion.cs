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
        => NullCheck.CheckIsNull(metadata);

    protected override string GetExpectation() => "to be null";
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
        => NullCheck.CheckIsNotNull(metadata);

    protected override string GetExpectation() => "to not be null";
}

internal class ListNullAssertion<TList, TItem> : ListAssertionBase<TList, TItem>
    where TList : IList<TItem>
{
    public ListNullAssertion(AssertionContext<TList> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TList> metadata)
        => NullCheck.CheckIsNull(metadata);

    protected override string GetExpectation() => "to be null";
}

internal class ListNotNullAssertion<TList, TItem> : ListAssertionBase<TList, TItem>
    where TList : IList<TItem>
{
    public ListNotNullAssertion(AssertionContext<TList> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TList> metadata)
        => NullCheck.CheckIsNotNull(metadata);

    protected override string GetExpectation() => "to not be null";
}

internal class ReadOnlyListNullAssertion<TList, TItem> : ReadOnlyListAssertionBase<TList, TItem>
    where TList : IReadOnlyList<TItem>
{
    public ReadOnlyListNullAssertion(AssertionContext<TList> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TList> metadata)
        => NullCheck.CheckIsNull(metadata);

    protected override string GetExpectation() => "to be null";
}

internal class ReadOnlyListNotNullAssertion<TList, TItem> : ReadOnlyListAssertionBase<TList, TItem>
    where TList : IReadOnlyList<TItem>
{
    public ReadOnlyListNotNullAssertion(AssertionContext<TList> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TList> metadata)
        => NullCheck.CheckIsNotNull(metadata);

    protected override string GetExpectation() => "to not be null";
}

internal class DictionaryNullAssertion<TDictionary, TKey, TValue> : DictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    public DictionaryNullAssertion(AssertionContext<TDictionary> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TDictionary> metadata)
        => NullCheck.CheckIsNull(metadata);

    protected override string GetExpectation() => "to be null";
}

internal class DictionaryNotNullAssertion<TDictionary, TKey, TValue> : DictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    public DictionaryNotNullAssertion(AssertionContext<TDictionary> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TDictionary> metadata)
        => NullCheck.CheckIsNotNull(metadata);

    protected override string GetExpectation() => "to not be null";
}

internal class MutableDictionaryNullAssertion<TDictionary, TKey, TValue> : MutableDictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IDictionary<TKey, TValue>
    where TKey : notnull
{
    public MutableDictionaryNullAssertion(AssertionContext<TDictionary> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TDictionary> metadata)
        => NullCheck.CheckIsNull(metadata);

    protected override string GetExpectation() => "to be null";
}

internal class MutableDictionaryNotNullAssertion<TDictionary, TKey, TValue> : MutableDictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IDictionary<TKey, TValue>
    where TKey : notnull
{
    public MutableDictionaryNotNullAssertion(AssertionContext<TDictionary> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TDictionary> metadata)
        => NullCheck.CheckIsNotNull(metadata);

    protected override string GetExpectation() => "to not be null";
}

internal class SetNullAssertion<TSet, TItem> : SetAssertionBase<TSet, TItem>
    where TSet : IEnumerable<TItem>
{
    private readonly Func<TSet, ISetAdapter<TItem>> _adapterFactory;

    public SetNullAssertion(
        AssertionContext<TSet> context,
        Func<TSet, ISetAdapter<TItem>> adapterFactory)
        : base(context)
    {
        _adapterFactory = adapterFactory;
    }

    protected override ISetAdapter<TItem> CreateSetAdapter(TSet value) => _adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TSet> metadata)
        => NullCheck.CheckIsNull(metadata);

    protected override string GetExpectation() => "to be null";
}

internal class SetNotNullAssertion<TSet, TItem> : SetAssertionBase<TSet, TItem>
    where TSet : IEnumerable<TItem>
{
    private readonly Func<TSet, ISetAdapter<TItem>> _adapterFactory;

    public SetNotNullAssertion(
        AssertionContext<TSet> context,
        Func<TSet, ISetAdapter<TItem>> adapterFactory)
        : base(context)
    {
        _adapterFactory = adapterFactory;
    }

    protected override ISetAdapter<TItem> CreateSetAdapter(TSet value) => _adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TSet> metadata)
        => NullCheck.CheckIsNotNull(metadata);

    protected override string GetExpectation() => "to not be null";
}

internal static class NullCheck
{
    public static Task<AssertionResult> CheckIsNull<TValue>(EvaluationMetadata<TValue> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}", metadata.Exception));
        }

        if (metadata.Value is null)
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed("value is not null"));
    }

    public static Task<AssertionResult> CheckIsNotNull<TValue>(EvaluationMetadata<TValue> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}", metadata.Exception));
        }

        if (metadata.Value is not null)
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed("value is null"));
    }
}
