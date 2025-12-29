using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that an async enumerable yields no items (is empty).
/// </summary>
public class AsyncEnumerableIsEmptyAssertion<TItem> : Assertion<IAsyncEnumerable<TItem>>
{
    public AsyncEnumerableIsEmptyAssertion(
        AssertionContext<IAsyncEnumerable<TItem>> context)
        : base(context)
    {
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<IAsyncEnumerable<TItem>> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return AssertionResult.Failed($"threw {exception.GetType().Name}");
        }

        if (value == null)
        {
            return AssertionResult.Failed("async enumerable was null");
        }

        var items = new List<TItem?>();
        const int maxItemsToShow = 10;
        var totalCount = 0;

        await foreach (var item in value)
        {
            totalCount++;
            if (items.Count < maxItemsToShow)
            {
                items.Add(item);
            }
        }

        if (totalCount == 0)
        {
            return AssertionResult.Passed;
        }

        var sb = new StringBuilder("async enumerable yielded items: [");
        sb.Append(string.Join(", ", items));
        if (totalCount > maxItemsToShow)
        {
            var remainingCount = totalCount - maxItemsToShow;
            sb.Append($", and {remainingCount} more...");
        }
        sb.Append(']');

        return AssertionResult.Failed(sb.ToString());
    }

    protected override string GetExpectation() => "to be empty";
}

/// <summary>
/// Asserts that an async enumerable yields at least one item (is not empty).
/// </summary>
public class AsyncEnumerableIsNotEmptyAssertion<TItem> : Assertion<IAsyncEnumerable<TItem>>
{
    public AsyncEnumerableIsNotEmptyAssertion(
        AssertionContext<IAsyncEnumerable<TItem>> context)
        : base(context)
    {
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<IAsyncEnumerable<TItem>> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return AssertionResult.Failed($"threw {exception.GetType().Name}");
        }

        if (value == null)
        {
            return AssertionResult.Failed("async enumerable was null");
        }

        await foreach (var _ in value)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("async enumerable was empty");
    }

    protected override string GetExpectation() => "to not be empty";
}

/// <summary>
/// Asserts that an async enumerable yields exactly the expected number of items.
/// </summary>
public class AsyncEnumerableYieldsCountAssertion<TItem> : Assertion<IAsyncEnumerable<TItem>>
{
    private readonly int _expectedCount;

    public AsyncEnumerableYieldsCountAssertion(
        AssertionContext<IAsyncEnumerable<TItem>> context,
        int expectedCount)
        : base(context)
    {
        _expectedCount = expectedCount;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<IAsyncEnumerable<TItem>> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return AssertionResult.Failed($"threw {exception.GetType().Name}");
        }

        if (value == null)
        {
            return AssertionResult.Failed("async enumerable was null");
        }

        var count = 0;
        await foreach (var _ in value)
        {
            count++;
        }

        if (count == _expectedCount)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"yielded {count} items");
    }

    protected override string GetExpectation() => $"to yield {_expectedCount} items";
}

/// <summary>
/// Asserts that an async enumerable yields exactly the expected items in order.
/// </summary>
public class AsyncEnumerableYieldsExactlyAssertion<TItem> : Assertion<IAsyncEnumerable<TItem>>
{
    private readonly TItem[] _expectedItems;
    private readonly IEqualityComparer<TItem>? _comparer;

    public AsyncEnumerableYieldsExactlyAssertion(
        AssertionContext<IAsyncEnumerable<TItem>> context,
        TItem[] expectedItems,
        IEqualityComparer<TItem>? comparer = null)
        : base(context)
    {
        _expectedItems = expectedItems;
        _comparer = comparer;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<IAsyncEnumerable<TItem>> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return AssertionResult.Failed($"threw {exception.GetType().Name}");
        }

        if (value == null)
        {
            return AssertionResult.Failed("async enumerable was null");
        }

        var comparer = _comparer ?? EqualityComparer<TItem>.Default;
        var actualItems = new List<TItem>();

        await foreach (var item in value)
        {
            actualItems.Add(item);
        }

        if (actualItems.Count != _expectedItems.Length)
        {
            return AssertionResult.Failed($"yielded {actualItems.Count} items: [{string.Join(", ", actualItems)}]");
        }

        for (int i = 0; i < actualItems.Count; i++)
        {
            if (!comparer.Equals(actualItems[i], _expectedItems[i]))
            {
                return AssertionResult.Failed($"item at index {i} was {actualItems[i]}, expected {_expectedItems[i]}");
            }
        }

        return AssertionResult.Passed;
    }

    protected override string GetExpectation() => $"to yield exactly [{string.Join(", ", _expectedItems.Select(i => i?.ToString() ?? "null"))}]";
}

/// <summary>
/// Asserts that an async enumerable contains the expected item.
/// </summary>
public class AsyncEnumerableContainsAssertion<TItem> : Assertion<IAsyncEnumerable<TItem>>
{
    private readonly TItem _expectedItem;
    private readonly IEqualityComparer<TItem>? _comparer;

    public AsyncEnumerableContainsAssertion(
        AssertionContext<IAsyncEnumerable<TItem>> context,
        TItem expectedItem,
        IEqualityComparer<TItem>? comparer = null)
        : base(context)
    {
        _expectedItem = expectedItem;
        _comparer = comparer;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<IAsyncEnumerable<TItem>> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return AssertionResult.Failed($"threw {exception.GetType().Name}");
        }

        if (value == null)
        {
            return AssertionResult.Failed("async enumerable was null");
        }

        var comparer = _comparer ?? EqualityComparer<TItem>.Default;

        await foreach (var item in value)
        {
            if (comparer.Equals(item, _expectedItem))
            {
                return AssertionResult.Passed;
            }
        }

        return AssertionResult.Failed($"did not contain {_expectedItem}");
    }

    protected override string GetExpectation() => $"to contain {_expectedItem}";
}

/// <summary>
/// Asserts that an async enumerable does not contain the expected item.
/// </summary>
public class AsyncEnumerableDoesNotContainAssertion<TItem> : Assertion<IAsyncEnumerable<TItem>>
{
    private readonly TItem _item;
    private readonly IEqualityComparer<TItem>? _comparer;

    public AsyncEnumerableDoesNotContainAssertion(
        AssertionContext<IAsyncEnumerable<TItem>> context,
        TItem item,
        IEqualityComparer<TItem>? comparer = null)
        : base(context)
    {
        _item = item;
        _comparer = comparer;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<IAsyncEnumerable<TItem>> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return AssertionResult.Failed($"threw {exception.GetType().Name}");
        }

        if (value == null)
        {
            return AssertionResult.Failed("async enumerable was null");
        }

        var comparer = _comparer ?? EqualityComparer<TItem>.Default;

        await foreach (var item in value)
        {
            if (comparer.Equals(item, _item))
            {
                return AssertionResult.Failed($"contained {_item}");
            }
        }

        return AssertionResult.Passed;
    }

    protected override string GetExpectation() => $"to not contain {_item}";
}

/// <summary>
/// Asserts that all items in an async enumerable satisfy a predicate.
/// </summary>
public class AsyncEnumerableAllAssertion<TItem> : Assertion<IAsyncEnumerable<TItem>>
{
    private readonly Func<TItem, bool> _predicate;
    private readonly string _predicateDescription;

    public AsyncEnumerableAllAssertion(
        AssertionContext<IAsyncEnumerable<TItem>> context,
        Func<TItem, bool> predicate,
        string predicateDescription)
        : base(context)
    {
        _predicate = predicate;
        _predicateDescription = predicateDescription;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<IAsyncEnumerable<TItem>> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return AssertionResult.Failed($"threw {exception.GetType().Name}");
        }

        if (value == null)
        {
            return AssertionResult.Failed("async enumerable was null");
        }

        int index = 0;
        await foreach (var item in value)
        {
            if (!_predicate(item))
            {
                return AssertionResult.Failed($"item at index {index} with value [{item}] does not satisfy predicate");
            }
            index++;
        }

        return AssertionResult.Passed;
    }

    protected override string GetExpectation() => $"all items to satisfy {_predicateDescription}";
}

/// <summary>
/// Asserts that at least one item in an async enumerable satisfies a predicate.
/// </summary>
public class AsyncEnumerableAnyAssertion<TItem> : Assertion<IAsyncEnumerable<TItem>>
{
    private readonly Func<TItem, bool> _predicate;
    private readonly string _predicateDescription;

    public AsyncEnumerableAnyAssertion(
        AssertionContext<IAsyncEnumerable<TItem>> context,
        Func<TItem, bool> predicate,
        string predicateDescription)
        : base(context)
    {
        _predicate = predicate;
        _predicateDescription = predicateDescription;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<IAsyncEnumerable<TItem>> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return AssertionResult.Failed($"threw {exception.GetType().Name}");
        }

        if (value == null)
        {
            return AssertionResult.Failed("async enumerable was null");
        }

        await foreach (var item in value)
        {
            if (_predicate(item))
            {
                return AssertionResult.Passed;
            }
        }

        return AssertionResult.Failed("no item satisfies predicate");
    }

    protected override string GetExpectation() => $"at least one item to satisfy {_predicateDescription}";
}

/// <summary>
/// Asserts that an async enumerable completes within the specified timeout.
/// </summary>
public class AsyncEnumerableCompletesWithinAssertion<TItem> : Assertion<IAsyncEnumerable<TItem>>
{
    private readonly TimeSpan _timeout;

    public AsyncEnumerableCompletesWithinAssertion(
        AssertionContext<IAsyncEnumerable<TItem>> context,
        TimeSpan timeout)
        : base(context)
    {
        _timeout = timeout;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<IAsyncEnumerable<TItem>> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return AssertionResult.Failed($"threw {exception.GetType().Name}");
        }

        if (value == null)
        {
            return AssertionResult.Failed("async enumerable was null");
        }

        using var cts = new CancellationTokenSource(_timeout);
        try
        {
            await foreach (var _ in value.WithCancellation(cts.Token))
            {
                // Just consume items until done or cancelled
            }
            return AssertionResult.Passed;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            return AssertionResult.Failed($"did not complete within {_timeout}");
        }
    }

    protected override string GetExpectation() => $"to complete within {_timeout}";
}

/// <summary>
/// Extension methods for IAsyncEnumerable assertions.
/// </summary>
public static class AsyncEnumerableAssertionExtensions
{
    /// <summary>
    /// Asserts that the async enumerable yields no items.
    /// </summary>
    public static AsyncEnumerableIsEmptyAssertion<TItem> IsEmpty<TItem>(
        this AsyncEnumerableAssertion<TItem> source)
    {
        source.Context.ExpressionBuilder.Append(".IsEmpty()");
        return new AsyncEnumerableIsEmptyAssertion<TItem>(source.Context);
    }

    /// <summary>
    /// Asserts that the async enumerable yields at least one item.
    /// </summary>
    public static AsyncEnumerableIsNotEmptyAssertion<TItem> IsNotEmpty<TItem>(
        this AsyncEnumerableAssertion<TItem> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotEmpty()");
        return new AsyncEnumerableIsNotEmptyAssertion<TItem>(source.Context);
    }

    /// <summary>
    /// Asserts that the async enumerable yields exactly the specified number of items.
    /// </summary>
    public static AsyncEnumerableYieldsCountAssertion<TItem> YieldsCount<TItem>(
        this AsyncEnumerableAssertion<TItem> source,
        int expectedCount,
        [CallerArgumentExpression(nameof(expectedCount))] string? expectedCountExpression = null)
    {
        source.Context.ExpressionBuilder.Append($".YieldsCount({expectedCountExpression})");
        return new AsyncEnumerableYieldsCountAssertion<TItem>(source.Context, expectedCount);
    }

    /// <summary>
    /// Asserts that the async enumerable yields exactly the specified items in order.
    /// </summary>
    public static AsyncEnumerableYieldsExactlyAssertion<TItem> YieldsExactly<TItem>(
        this AsyncEnumerableAssertion<TItem> source,
        params TItem[] expectedItems)
    {
        source.Context.ExpressionBuilder.Append($".YieldsExactly([{string.Join(", ", expectedItems.Select(i => i?.ToString() ?? "null"))}])");
        return new AsyncEnumerableYieldsExactlyAssertion<TItem>(source.Context, expectedItems);
    }

    /// <summary>
    /// Asserts that the async enumerable yields exactly the specified items in order using a custom comparer.
    /// </summary>
    public static AsyncEnumerableYieldsExactlyAssertion<TItem> YieldsExactly<TItem>(
        this AsyncEnumerableAssertion<TItem> source,
        IEqualityComparer<TItem> comparer,
        params TItem[] expectedItems)
    {
        source.Context.ExpressionBuilder.Append($".YieldsExactly([{string.Join(", ", expectedItems.Select(i => i?.ToString() ?? "null"))}])");
        return new AsyncEnumerableYieldsExactlyAssertion<TItem>(source.Context, expectedItems, comparer);
    }

    /// <summary>
    /// Asserts that the async enumerable contains the specified item.
    /// </summary>
    public static AsyncEnumerableContainsAssertion<TItem> Contains<TItem>(
        this AsyncEnumerableAssertion<TItem> source,
        TItem expectedItem,
        [CallerArgumentExpression(nameof(expectedItem))] string? expectedItemExpression = null)
    {
        source.Context.ExpressionBuilder.Append($".Contains({expectedItemExpression})");
        return new AsyncEnumerableContainsAssertion<TItem>(source.Context, expectedItem);
    }

    /// <summary>
    /// Asserts that the async enumerable contains the specified item using a custom comparer.
    /// </summary>
    public static AsyncEnumerableContainsAssertion<TItem> Contains<TItem>(
        this AsyncEnumerableAssertion<TItem> source,
        TItem expectedItem,
        IEqualityComparer<TItem> comparer,
        [CallerArgumentExpression(nameof(expectedItem))] string? expectedItemExpression = null)
    {
        source.Context.ExpressionBuilder.Append($".Contains({expectedItemExpression})");
        return new AsyncEnumerableContainsAssertion<TItem>(source.Context, expectedItem, comparer);
    }

    /// <summary>
    /// Asserts that the async enumerable does not contain the specified item.
    /// </summary>
    public static AsyncEnumerableDoesNotContainAssertion<TItem> DoesNotContain<TItem>(
        this AsyncEnumerableAssertion<TItem> source,
        TItem item,
        [CallerArgumentExpression(nameof(item))] string? itemExpression = null)
    {
        source.Context.ExpressionBuilder.Append($".DoesNotContain({itemExpression})");
        return new AsyncEnumerableDoesNotContainAssertion<TItem>(source.Context, item);
    }

    /// <summary>
    /// Asserts that the async enumerable does not contain the specified item using a custom comparer.
    /// </summary>
    public static AsyncEnumerableDoesNotContainAssertion<TItem> DoesNotContain<TItem>(
        this AsyncEnumerableAssertion<TItem> source,
        TItem item,
        IEqualityComparer<TItem> comparer,
        [CallerArgumentExpression(nameof(item))] string? itemExpression = null)
    {
        source.Context.ExpressionBuilder.Append($".DoesNotContain({itemExpression})");
        return new AsyncEnumerableDoesNotContainAssertion<TItem>(source.Context, item, comparer);
    }

    /// <summary>
    /// Asserts that all items yielded by the async enumerable satisfy the given predicate.
    /// </summary>
    public static AsyncEnumerableAllAssertion<TItem> All<TItem>(
        this AsyncEnumerableAssertion<TItem> source,
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? predicateExpression = null)
    {
        source.Context.ExpressionBuilder.Append($".All({predicateExpression})");
        return new AsyncEnumerableAllAssertion<TItem>(source.Context, predicate, predicateExpression ?? "predicate");
    }

    /// <summary>
    /// Asserts that at least one item yielded by the async enumerable satisfies the given predicate.
    /// </summary>
    public static AsyncEnumerableAnyAssertion<TItem> Any<TItem>(
        this AsyncEnumerableAssertion<TItem> source,
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? predicateExpression = null)
    {
        source.Context.ExpressionBuilder.Append($".Any({predicateExpression})");
        return new AsyncEnumerableAnyAssertion<TItem>(source.Context, predicate, predicateExpression ?? "predicate");
    }

    /// <summary>
    /// Asserts that the async enumerable completes within the specified timeout.
    /// This is useful for testing that an async sequence doesn't hang indefinitely.
    /// </summary>
    public static AsyncEnumerableCompletesWithinAssertion<TItem> CompletesWithin<TItem>(
        this AsyncEnumerableAssertion<TItem> source,
        TimeSpan timeout,
        [CallerArgumentExpression(nameof(timeout))] string? timeoutExpression = null)
    {
        source.Context.ExpressionBuilder.Append($".CompletesWithin({timeoutExpression})");
        return new AsyncEnumerableCompletesWithinAssertion<TItem>(source.Context, timeout);
    }
}
