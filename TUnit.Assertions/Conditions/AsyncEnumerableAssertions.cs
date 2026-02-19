using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Base class for async enumerable assertions that handles materialization.
/// </summary>
public abstract class AsyncEnumerableAssertionConditionBase<TItem> : AsyncEnumerableAssertionBase<TItem>
{
    protected AsyncEnumerableAssertionConditionBase(AssertionContext<IAsyncEnumerable<TItem>> context)
        : base(context)
    {
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<IAsyncEnumerable<TItem>> metadata)
    {
        if (metadata.Exception != null)
        {
            return AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}");
        }

        if (metadata.Value == null)
        {
            return AssertionResult.Failed("received null");
        }

        // Materialize the async enumerable
        var items = await MaterializeAsync(metadata.Value).ConfigureAwait(false);
        return CheckMaterialized(items);
    }

    private static async Task<List<TItem>> MaterializeAsync(IAsyncEnumerable<TItem> source)
    {
        var list = new List<TItem>();
        await foreach (var item in source.ConfigureAwait(false))
        {
            list.Add(item);
        }
        return list;
    }

    protected abstract AssertionResult CheckMaterialized(List<TItem> items);
}

/// <summary>
/// Asserts that the async enumerable is empty or not empty.
/// </summary>
public class AsyncEnumerableIsEmptyAssertion<TItem> : AsyncEnumerableAssertionConditionBase<TItem>
{
    private readonly bool _expectEmpty;

    public AsyncEnumerableIsEmptyAssertion(
        AssertionContext<IAsyncEnumerable<TItem>> context,
        bool expectEmpty)
        : base(context)
    {
        _expectEmpty = expectEmpty;
    }

    protected override AssertionResult CheckMaterialized(List<TItem> items)
    {
        var isEmpty = items.Count == 0;

        if (_expectEmpty)
        {
            if (isEmpty)
            {
                return AssertionResult.Passed;
            }

            var preview = items.Count <= 10
                ? string.Join(", ", items)
                : string.Join(", ", items.Take(10)) + $", and {items.Count - 10} more...";
            return AssertionResult.Failed($"async enumerable contains items: [{preview}]");
        }
        else
        {
            return isEmpty
                ? AssertionResult.Failed("async enumerable was empty")
                : AssertionResult.Passed;
        }
    }

    protected override string GetExpectation() =>
        _expectEmpty ? "to be empty" : "to not be empty";
}

/// <summary>
/// Asserts that the async enumerable has exactly the expected count of items.
/// </summary>
public class AsyncEnumerableHasCountAssertion<TItem> : AsyncEnumerableAssertionConditionBase<TItem>
{
    private readonly int _expected;

    public AsyncEnumerableHasCountAssertion(
        AssertionContext<IAsyncEnumerable<TItem>> context,
        int expected)
        : base(context)
    {
        _expected = expected;
    }

    protected override AssertionResult CheckMaterialized(List<TItem> items)
    {
        return items.Count == _expected
            ? AssertionResult.Passed
            : AssertionResult.Failed($"received {items.Count} items");
    }

    protected override string GetExpectation() => $"to have {_expected} items";
}

/// <summary>
/// Asserts that the async enumerable has at least the specified minimum number of items.
/// </summary>
public class AsyncEnumerableHasAtLeastAssertion<TItem> : AsyncEnumerableAssertionConditionBase<TItem>
{
    private readonly int _minCount;

    public AsyncEnumerableHasAtLeastAssertion(
        AssertionContext<IAsyncEnumerable<TItem>> context,
        int minCount)
        : base(context)
    {
        _minCount = minCount;
    }

    protected override AssertionResult CheckMaterialized(List<TItem> items)
    {
        return items.Count >= _minCount
            ? AssertionResult.Passed
            : AssertionResult.Failed($"found {items.Count} items");
    }

    protected override string GetExpectation() => $"to have at least {_minCount} item(s)";
}

/// <summary>
/// Asserts that the async enumerable has at most the specified maximum number of items.
/// </summary>
public class AsyncEnumerableHasAtMostAssertion<TItem> : AsyncEnumerableAssertionConditionBase<TItem>
{
    private readonly int _maxCount;

    public AsyncEnumerableHasAtMostAssertion(
        AssertionContext<IAsyncEnumerable<TItem>> context,
        int maxCount)
        : base(context)
    {
        _maxCount = maxCount;
    }

    protected override AssertionResult CheckMaterialized(List<TItem> items)
    {
        return items.Count <= _maxCount
            ? AssertionResult.Passed
            : AssertionResult.Failed($"found {items.Count} items");
    }

    protected override string GetExpectation() => $"to have at most {_maxCount} item(s)";
}

/// <summary>
/// Asserts that the async enumerable count is between the specified minimum and maximum (inclusive).
/// </summary>
public class AsyncEnumerableHasCountBetweenAssertion<TItem> : AsyncEnumerableAssertionConditionBase<TItem>
{
    private readonly int _min;
    private readonly int _max;

    public AsyncEnumerableHasCountBetweenAssertion(
        AssertionContext<IAsyncEnumerable<TItem>> context,
        int min,
        int max)
        : base(context)
    {
        _min = min;
        _max = max;
    }

    protected override AssertionResult CheckMaterialized(List<TItem> items)
    {
        return items.Count >= _min && items.Count <= _max
            ? AssertionResult.Passed
            : AssertionResult.Failed($"found {items.Count} items");
    }

    protected override string GetExpectation() => $"to have count between {_min} and {_max}";
}

/// <summary>
/// Asserts that the async enumerable contains or does not contain the expected item.
/// </summary>
public class AsyncEnumerableContainsAssertion<TItem> : AsyncEnumerableAssertionConditionBase<TItem>
{
    private readonly TItem _expected;
    private readonly IEqualityComparer<TItem>? _comparer;
    private readonly bool _expectContains;

    public AsyncEnumerableContainsAssertion(
        AssertionContext<IAsyncEnumerable<TItem>> context,
        TItem expected,
        IEqualityComparer<TItem>? comparer,
        bool expectContains)
        : base(context)
    {
        _expected = expected;
        _comparer = comparer;
        _expectContains = expectContains;
    }

    protected override AssertionResult CheckMaterialized(List<TItem> items)
    {
        var comparer = _comparer ?? EqualityComparer<TItem>.Default;
        var contains = items.Any(item => comparer.Equals(item, _expected));

        if (_expectContains)
        {
            return contains
                ? AssertionResult.Passed
                : AssertionResult.Failed($"{_expected} was not found in the collection");
        }
        else
        {
            return contains
                ? AssertionResult.Failed($"{_expected} was found in the collection")
                : AssertionResult.Passed;
        }
    }

    protected override string GetExpectation() =>
        _expectContains ? $"to contain {_expected}" : $"to not contain {_expected}";
}

/// <summary>
/// Asserts that all items in the async enumerable satisfy the predicate.
/// </summary>
public class AsyncEnumerableAllAssertion<TItem> : AsyncEnumerableAssertionConditionBase<TItem>
{
    private readonly Func<TItem, bool> _predicate;

    public AsyncEnumerableAllAssertion(
        AssertionContext<IAsyncEnumerable<TItem>> context,
        Func<TItem, bool> predicate)
        : base(context)
    {
        _predicate = predicate;
    }

    protected override AssertionResult CheckMaterialized(List<TItem> items)
    {
        var failingItems = items.Where(item => !_predicate(item)).ToList();

        if (failingItems.Count == 0)
        {
            return AssertionResult.Passed;
        }

        var preview = failingItems.Count <= 5
            ? string.Join(", ", failingItems)
            : string.Join(", ", failingItems.Take(5)) + $", and {failingItems.Count - 5} more...";
        return AssertionResult.Failed($"{failingItems.Count} items did not satisfy the predicate: [{preview}]");
    }

    protected override string GetExpectation() => "all items to satisfy the predicate";
}

/// <summary>
/// Asserts that any item in the async enumerable satisfies the predicate.
/// </summary>
public class AsyncEnumerableAnyAssertion<TItem> : AsyncEnumerableAssertionConditionBase<TItem>
{
    private readonly Func<TItem, bool> _predicate;

    public AsyncEnumerableAnyAssertion(
        AssertionContext<IAsyncEnumerable<TItem>> context,
        Func<TItem, bool> predicate)
        : base(context)
    {
        _predicate = predicate;
    }

    protected override AssertionResult CheckMaterialized(List<TItem> items)
    {
        return items.Any(_predicate)
            ? AssertionResult.Passed
            : AssertionResult.Failed("no items satisfied the predicate");
    }

    protected override string GetExpectation() => "at least one item to satisfy the predicate";
}
