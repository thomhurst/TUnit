using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a collection/enumerable is empty.
/// </summary>
public class CollectionIsEmptyAssertion<TValue> : Assertion<TValue>
    where TValue : IEnumerable
{
    public CollectionIsEmptyAssertion(
        AssertionContext<TValue> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var enumerator = value.GetEnumerator();
        try
        {
            if (!enumerator.MoveNext())
            {
                return Task.FromResult(AssertionResult.Passed);
            }

            // Collection is not empty - collect items for error message
            var items = new List<object?>();
            const int maxItemsToShow = 10;
            var totalCount = 1; // We already have the first item

            // Add first item
            items.Add(enumerator.Current);

            // Collect remaining items up to the limit
            while (enumerator.MoveNext())
            {
                totalCount++;
                if (items.Count < maxItemsToShow)
                {
                    items.Add(enumerator.Current);
                }
            }

            // Build error message
            var sb = new StringBuilder("collection contains items: [");
            sb.Append(string.Join(", ", items));
            if (totalCount > maxItemsToShow)
            {
                var remainingCount = totalCount - maxItemsToShow;
                sb.Append($", and {remainingCount} more...");
            }
            sb.Append(']');

            return Task.FromResult(AssertionResult.Failed(sb.ToString()));
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }

    protected override string GetExpectation() => "to be empty";
}

/// <summary>
/// Asserts that a collection/enumerable is NOT empty.
/// </summary>
public class CollectionIsNotEmptyAssertion<TValue> : Assertion<TValue>
    where TValue : IEnumerable
{
    public CollectionIsNotEmptyAssertion(
        AssertionContext<TValue> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var enumerator = value.GetEnumerator();
        try
        {
            if (enumerator.MoveNext())
            {
                return Task.FromResult(AssertionResult.Passed);
            }

            return Task.FromResult(AssertionResult.Failed("collection is empty"));
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }

    protected override string GetExpectation() => "to not be empty";
}

/// <summary>
/// Asserts that a collection contains the expected item.
/// </summary>
public class CollectionContainsAssertion<TCollection, TItem> : Assertion<TCollection>
    where TCollection : IEnumerable<TItem>
{
    private readonly TItem _expected;
    private readonly IEqualityComparer<TItem>? _comparer;

    public CollectionContainsAssertion(
        AssertionContext<TCollection> context,
        TItem expected,
        IEqualityComparer<TItem>? comparer = null)
        : base(context)
    {
        _expected = expected;
        _comparer = comparer;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var comparer = _comparer ?? EqualityComparer<TItem>.Default;

        foreach (var item in value)
        {
            if (comparer.Equals(item, _expected))
            {
                return Task.FromResult(AssertionResult.Passed);
            }
        }

        return Task.FromResult(AssertionResult.Failed($"item not found in collection"));
    }

    protected override string GetExpectation() => $"to contain {_expected}";
}

/// <summary>
/// Asserts that a collection does NOT contain the expected item.
/// </summary>
public class CollectionDoesNotContainAssertion<TCollection, TItem> : Assertion<TCollection>
    where TCollection : IEnumerable<TItem>
{
    private readonly TItem _expected;
    private readonly IEqualityComparer<TItem>? _comparer;

    public CollectionDoesNotContainAssertion(
        AssertionContext<TCollection> context,
        TItem expected,
        IEqualityComparer<TItem>? comparer = null)
        : base(context)
    {
        _expected = expected;
        _comparer = comparer;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var comparer = _comparer ?? EqualityComparer<TItem>.Default;

        foreach (var item in value)
        {
            if (comparer.Equals(item, _expected))
            {
                return Task.FromResult(AssertionResult.Failed($"found {_expected} in collection"));
            }
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => $"to not contain {_expected}";
}

/// <summary>
/// Asserts that a collection does NOT contain any item matching the predicate.
/// </summary>
public class CollectionDoesNotContainPredicateAssertion<TCollection, TItem> : Assertion<TCollection>
    where TCollection : IEnumerable<TItem>
{
    private readonly Func<TItem, bool> _predicate;
    private readonly string _predicateDescription;

    public CollectionDoesNotContainPredicateAssertion(
        AssertionContext<TCollection> context,
        Func<TItem, bool> predicate,
        string predicateDescription)
        : base(context)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        _predicateDescription = predicateDescription;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        foreach (var item in value)
        {
            if (_predicate(item))
            {
                return Task.FromResult(AssertionResult.Failed($"found item matching predicate"));
            }
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => $"to not contain any item matching {_predicateDescription}";
}

/// <summary>
/// Asserts that a collection has a specific count/length.
/// </summary>
public class CollectionCountAssertion<TValue> : Assertion<TValue>
    where TValue : IEnumerable
{
    private readonly int _expectedCount;

    public CollectionCountAssertion(
        AssertionContext<TValue> context,
        int expectedCount)
        : base(context)
    {
        _expectedCount = expectedCount;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        // Try to get count efficiently
        int actualCount;
        if (value is ICollection collection)
        {
            actualCount = collection.Count;
        }
        else
        {
            actualCount = 0;
            var enumerator = value.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                    actualCount++;
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }

        if (actualCount == _expectedCount)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"found count {actualCount}"));
    }

    protected override string GetExpectation() => $"to have count {_expectedCount}";
}

/// <summary>
/// Asserts that all items in a collection satisfy a predicate.
/// </summary>
/// <summary>
/// Helper for All().Satisfy() pattern - allows custom assertions on all collection items.
/// </summary>
public class CollectionAllSatisfyHelper<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    private readonly AssertionContext<TCollection> _context;

    public CollectionAllSatisfyHelper(AssertionContext<TCollection> context)
    {
        _context = context;
    }

    /// <summary>
    /// Asserts that all items satisfy the given assertion.
    /// Example: .All().Satisfy(item => item.IsNotNull())
    /// </summary>
    public CollectionAllSatisfyAssertion<TCollection, TItem> Satisfy(
        Func<IAssertionSource<TItem>, Assertion<TItem>?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
    {
        _context.ExpressionBuilder.Append($".Satisfy({expression})");
        return new CollectionAllSatisfyAssertion<TCollection, TItem>(_context, assertion, expression ?? "assertion");
    }

    /// <summary>
    /// Asserts that all items, when mapped, satisfy the given assertion.
    /// Example: .All().Satisfy(model => model.Value, assert => assert.IsNotNull())
    /// </summary>
    public CollectionAllSatisfyMappedAssertion<TCollection, TItem, TMapped> Satisfy<TMapped>(
        Func<TItem, TMapped> mapper,
        Func<IAssertionSource<TMapped>, Assertion<TMapped>?> assertion,
        [CallerArgumentExpression(nameof(mapper))] string? mapperExpression = null,
        [CallerArgumentExpression(nameof(assertion))] string? assertionExpression = null)
    {
        _context.ExpressionBuilder.Append($".Satisfy({mapperExpression}, {assertionExpression})");
        return new CollectionAllSatisfyMappedAssertion<TCollection, TItem, TMapped>(
            _context, mapper, assertion, mapperExpression ?? "mapper", assertionExpression ?? "assertion");
    }
}

public class CollectionAllAssertion<TCollection, TItem> : Assertion<TCollection>
    where TCollection : IEnumerable<TItem>
{
    private readonly Func<TItem, bool> _predicate;
    private readonly string _predicateDescription;

    public CollectionAllAssertion(
        AssertionContext<TCollection> context,
        Func<TItem, bool> predicate,
        string predicateDescription)
        : base(context)
    {
        _predicate = predicate;
        _predicateDescription = predicateDescription;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        int index = 0;
        foreach (var item in value)
        {
            if (!_predicate(item))
            {
                return Task.FromResult(AssertionResult.Failed($"item at index {index} does not satisfy predicate"));
            }

            index++;
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => $"all items to satisfy {_predicateDescription}";
}

/// <summary>
/// Asserts that at least one item in a collection satisfies a predicate.
/// </summary>
public class CollectionAnyAssertion<TCollection, TItem> : Assertion<TCollection>
    where TCollection : IEnumerable<TItem>
{
    private readonly Func<TItem, bool> _predicate;
    private readonly string _predicateDescription;

    public CollectionAnyAssertion(
        AssertionContext<TCollection> context,
        Func<TItem, bool> predicate,
        string predicateDescription)
        : base(context)
    {
        _predicate = predicate;
        _predicateDescription = predicateDescription;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        foreach (var item in value)
        {
            if (_predicate(item))
            {
                return Task.FromResult(AssertionResult.Passed);
            }
        }

        return Task.FromResult(AssertionResult.Failed("no item satisfies predicate"));
    }

    protected override string GetExpectation() => $"at least one item to satisfy {_predicateDescription}";
}

/// <summary>
/// Asserts that a collection contains exactly one item.
/// </summary>
public class HasSingleItemAssertion<TValue> : Assertion<TValue>
    where TValue : IEnumerable
{
    public HasSingleItemAssertion(
        AssertionContext<TValue> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var enumerator = value.GetEnumerator();
        try
        {
            if (!enumerator.MoveNext())
            {
                return Task.FromResult(AssertionResult.Failed("collection is empty"));
            }

            // First item exists, check if there's a second
            if (enumerator.MoveNext())
            {
                return Task.FromResult(AssertionResult.Failed("collection has more than one item"));
            }

            return Task.FromResult(AssertionResult.Passed);
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }

    protected override string GetExpectation() => "to have exactly one item";
}

/// <summary>
/// Asserts that a collection contains an item matching the predicate.
/// </summary>
public class CollectionContainsPredicateAssertion<TCollection, TItem> : Assertion<TItem>
    where TCollection : IEnumerable<TItem>
{
    private readonly Func<TItem, bool> _predicate;

    public CollectionContainsPredicateAssertion(
        AssertionContext<TCollection> context,
        Func<TItem, bool> predicate)
        : base(context.Map<TItem>(collection =>
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection), "collection was null");
            }

            foreach (var item in collection)
            {
                if (predicate(item))
                {
                    return item;
                }
            }

            throw new InvalidOperationException("no item matching predicate found in collection");
        }))
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TItem> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed(exception.Message));
        }

        // If we got here, the item was found (the Map function succeeded)
        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to contain item matching predicate";
}

/// <summary>
/// Asserts that all items in the collection satisfy a custom assertion.
/// </summary>
public class CollectionAllSatisfyAssertion<TCollection, TItem> : Assertion<TCollection>
    where TCollection : IEnumerable<TItem>
{
    private readonly Func<IAssertionSource<TItem>, Assertion<TItem>?> _assertion;
    private readonly string _assertionDescription;

    public CollectionAllSatisfyAssertion(
        AssertionContext<TCollection> context,
        Func<IAssertionSource<TItem>, Assertion<TItem>?> assertion,
        string assertionDescription)
        : base(context)
    {
        _assertion = assertion;
        _assertionDescription = assertionDescription;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return AssertionResult.Failed($"threw {exception.GetType().Name}");
        }

        if (value == null)
        {
            return AssertionResult.Failed("collection was null");
        }

        int index = 0;
        foreach (var item in value)
        {
            var itemAssertion = new ValueAssertion<TItem>(item, $"item[{index}]");
            try
            {
                // Execute the assertion function and await the result
                var assertion = _assertion(itemAssertion);
                if (assertion != null)
                {
                    await assertion.AssertAsync();
                }
            }
            catch (Exception ex)
            {
                return AssertionResult.Failed($"item at index {index} failed assertion: {ex.Message}");
            }
            index++;
        }

        return AssertionResult.Passed;
    }

    protected override string GetExpectation() => $"all items to satisfy {_assertionDescription}";
}

/// <summary>
/// Asserts that all items in the collection, when mapped, satisfy a custom assertion.
/// </summary>
public class CollectionAllSatisfyMappedAssertion<TCollection, TItem, TMapped> : Assertion<TCollection>
    where TCollection : IEnumerable<TItem>
{
    private readonly Func<TItem, TMapped> _mapper;
    private readonly Func<IAssertionSource<TMapped>, Assertion<TMapped>?> _assertion;
    private readonly string _mapperDescription;
    private readonly string _assertionDescription;

    public CollectionAllSatisfyMappedAssertion(
        AssertionContext<TCollection> context,
        Func<TItem, TMapped> mapper,
        Func<IAssertionSource<TMapped>, Assertion<TMapped>?> assertion,
        string mapperDescription,
        string assertionDescription)
        : base(context)
    {
        _mapper = mapper;
        _assertion = assertion;
        _mapperDescription = mapperDescription;
        _assertionDescription = assertionDescription;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return AssertionResult.Failed($"threw {exception.GetType().Name}");
        }

        if (value == null)
        {
            return AssertionResult.Failed("collection was null");
        }

        int index = 0;
        foreach (var item in value)
        {
            var mappedValue = _mapper(item);
            var mappedAssertion = new ValueAssertion<TMapped>(mappedValue, $"mapped[{index}]");
            try
            {
                var resultingAssertion = _assertion(mappedAssertion);
                if (resultingAssertion != null)
                {
                    await resultingAssertion.AssertAsync();
                }
            }
            catch (Exception ex)
            {
                return AssertionResult.Failed($"item at index {index} (mapped by {_mapperDescription}) failed assertion: {ex.Message}");
            }
            index++;
        }

        return AssertionResult.Passed;
    }

    protected override string GetExpectation() => $"items mapped by {_mapperDescription} to satisfy {_assertionDescription}";
}

/// <summary>
/// Asserts that a collection is in ascending order.
/// </summary>
public class CollectionIsInOrderAssertion<TCollection, TItem> : Assertion<TCollection>
    where TCollection : IEnumerable<TItem>
    where TItem : IComparable<TItem>
{
    public CollectionIsInOrderAssertion(
        AssertionContext<TCollection> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("collection was null"));
        }

        TItem? previous = default;
        bool first = true;
        int index = 0;

        foreach (var item in value)
        {
            if (!first && previous != null)
            {
                if (previous.CompareTo(item) > 0)
                {
                    return Task.FromResult(AssertionResult.Failed($"item at index {index} ({item}) is less than previous item ({previous})"));
                }
            }

            previous = item;
            first = false;
            index++;
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be in ascending order";
}

/// <summary>
/// Asserts that a collection is in descending order.
/// </summary>
public class CollectionIsInDescendingOrderAssertion<TCollection, TItem> : Assertion<TCollection>
    where TCollection : IEnumerable<TItem>
    where TItem : IComparable<TItem>
{
    public CollectionIsInDescendingOrderAssertion(
        AssertionContext<TCollection> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("collection was null"));
        }

        TItem? previous = default;
        bool first = true;
        int index = 0;

        foreach (var item in value)
        {
            if (!first && previous != null)
            {
                if (previous.CompareTo(item) < 0)
                {
                    return Task.FromResult(AssertionResult.Failed($"item at index {index} ({item}) is greater than previous item ({previous})"));
                }
            }

            previous = item;
            first = false;
            index++;
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be in descending order";
}
