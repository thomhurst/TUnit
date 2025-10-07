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
        EvaluationContext<TValue> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

        var enumerator = value.GetEnumerator();
        try
        {
            if (!enumerator.MoveNext())
                return Task.FromResult(AssertionResult.Passed);

            return Task.FromResult(AssertionResult.Failed("collection contains items"));
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
        EvaluationContext<TValue> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

        var enumerator = value.GetEnumerator();
        try
        {
            if (enumerator.MoveNext())
                return Task.FromResult(AssertionResult.Passed);

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
        EvaluationContext<TCollection> context,
        TItem expected,
        StringBuilder expressionBuilder,
        IEqualityComparer<TItem>? comparer = null)
        : base(context, expressionBuilder)
    {
        _expected = expected;
        _comparer = comparer;
    }

    protected override Task<AssertionResult> CheckAsync(TCollection? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

        var comparer = _comparer ?? EqualityComparer<TItem>.Default;

        foreach (var item in value)
        {
            if (comparer.Equals(item, _expected))
                return Task.FromResult(AssertionResult.Passed);
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
        EvaluationContext<TCollection> context,
        TItem expected,
        StringBuilder expressionBuilder,
        IEqualityComparer<TItem>? comparer = null)
        : base(context, expressionBuilder)
    {
        _expected = expected;
        _comparer = comparer;
    }

    protected override Task<AssertionResult> CheckAsync(TCollection? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

        var comparer = _comparer ?? EqualityComparer<TItem>.Default;

        foreach (var item in value)
        {
            if (comparer.Equals(item, _expected))
                return Task.FromResult(AssertionResult.Failed($"found {_expected} in collection"));
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
        EvaluationContext<TCollection> context,
        Func<TItem, bool> predicate,
        string predicateDescription,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        _predicateDescription = predicateDescription;
    }

    protected override Task<AssertionResult> CheckAsync(TCollection? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

        foreach (var item in value)
        {
            if (_predicate(item))
                return Task.FromResult(AssertionResult.Failed($"found item matching predicate"));
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
        EvaluationContext<TValue> context,
        int expectedCount,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _expectedCount = expectedCount;
    }

    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

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
            return Task.FromResult(AssertionResult.Passed);

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
    private readonly EvaluationContext<TCollection> _context;
    private readonly StringBuilder _expressionBuilder;

    public CollectionAllSatisfyHelper(EvaluationContext<TCollection> context, StringBuilder expressionBuilder)
    {
        _context = context;
        _expressionBuilder = expressionBuilder;
    }

    /// <summary>
    /// Asserts that all items satisfy the given assertion.
    /// Example: .All().Satisfy(item => item.IsNotNull())
    /// </summary>
    public CollectionAllSatisfyAssertion<TCollection, TItem> Satisfy(
        Action<IAssertionSource<TItem>> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
    {
        _expressionBuilder.Append($".Satisfy({expression})");
        return new CollectionAllSatisfyAssertion<TCollection, TItem>(_context, assertion, expression ?? "assertion", _expressionBuilder);
    }

    /// <summary>
    /// Asserts that all items, when mapped, satisfy the given assertion.
    /// Example: .All().Satisfy(model => model.Value, value => value.IsNotNull())
    /// </summary>
    public CollectionAllSatisfyMappedAssertion<TCollection, TItem, TMapped> Satisfy<TMapped>(
        Func<TItem, TMapped> mapper,
        Action<IAssertionSource<TMapped>> assertion,
        [CallerArgumentExpression(nameof(mapper))] string? mapperExpression = null,
        [CallerArgumentExpression(nameof(assertion))] string? assertionExpression = null)
    {
        _expressionBuilder.Append($".Satisfy({mapperExpression}, {assertionExpression})");
        return new CollectionAllSatisfyMappedAssertion<TCollection, TItem, TMapped>(
            _context, mapper, assertion, mapperExpression ?? "mapper", assertionExpression ?? "assertion", _expressionBuilder);
    }
}

public class CollectionAllAssertion<TCollection, TItem> : Assertion<TCollection>
    where TCollection : IEnumerable<TItem>
{
    private readonly Func<TItem, bool> _predicate;
    private readonly string _predicateDescription;

    public CollectionAllAssertion(
        EvaluationContext<TCollection> context,
        Func<TItem, bool> predicate,
        string predicateDescription,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _predicate = predicate;
        _predicateDescription = predicateDescription;
    }

    protected override Task<AssertionResult> CheckAsync(TCollection? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

        int index = 0;
        foreach (var item in value)
        {
            if (!_predicate(item))
                return Task.FromResult(AssertionResult.Failed($"item at index {index} does not satisfy predicate"));
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
        EvaluationContext<TCollection> context,
        Func<TItem, bool> predicate,
        string predicateDescription,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _predicate = predicate;
        _predicateDescription = predicateDescription;
    }

    protected override Task<AssertionResult> CheckAsync(TCollection? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

        foreach (var item in value)
        {
            if (_predicate(item))
                return Task.FromResult(AssertionResult.Passed);
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
        EvaluationContext<TValue> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

        var enumerator = value.GetEnumerator();
        try
        {
            if (!enumerator.MoveNext())
                return Task.FromResult(AssertionResult.Failed("collection is empty"));

            // First item exists, check if there's a second
            if (enumerator.MoveNext())
                return Task.FromResult(AssertionResult.Failed("collection has more than one item"));

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
public class CollectionContainsPredicateAssertion<TCollection, TItem> : Assertion<TCollection>
    where TCollection : IEnumerable<TItem>
{
    private readonly Func<TItem, bool> _predicate;

    public CollectionContainsPredicateAssertion(
        EvaluationContext<TCollection> context,
        Func<TItem, bool> predicate,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    protected override Task<AssertionResult> CheckAsync(TCollection? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

        foreach (var item in value)
        {
            if (_predicate(item))
                return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed("no item matching predicate found in collection"));
    }

    protected override string GetExpectation() => "to contain item matching predicate";
}

/// <summary>
/// Asserts that all items in the collection satisfy a custom assertion.
/// </summary>
public class CollectionAllSatisfyAssertion<TCollection, TItem> : Assertion<TCollection>
    where TCollection : IEnumerable<TItem>
{
    private readonly Action<IAssertionSource<TItem>> _assertion;
    private readonly string _assertionDescription;

    public CollectionAllSatisfyAssertion(
        EvaluationContext<TCollection> context,
        Action<IAssertionSource<TItem>> assertion,
        string assertionDescription,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _assertion = assertion;
        _assertionDescription = assertionDescription;
    }

    protected override Task<AssertionResult> CheckAsync(TCollection? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("collection was null"));

        int index = 0;
        foreach (var item in value)
        {
            var itemAssertion = new ValueAssertion<TItem>(item, $"item[{index}]");
            try
            {
                _assertion(itemAssertion);
            }
            catch (Exception ex)
            {
                return Task.FromResult(AssertionResult.Failed($"item at index {index} failed assertion: {ex.Message}"));
            }
            index++;
        }

        return Task.FromResult(AssertionResult.Passed);
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
    private readonly Action<IAssertionSource<TMapped>> _assertion;
    private readonly string _mapperDescription;
    private readonly string _assertionDescription;

    public CollectionAllSatisfyMappedAssertion(
        EvaluationContext<TCollection> context,
        Func<TItem, TMapped> mapper,
        Action<IAssertionSource<TMapped>> assertion,
        string mapperDescription,
        string assertionDescription,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _mapper = mapper;
        _assertion = assertion;
        _mapperDescription = mapperDescription;
        _assertionDescription = assertionDescription;
    }

    protected override Task<AssertionResult> CheckAsync(TCollection? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("collection was null"));

        int index = 0;
        foreach (var item in value)
        {
            var mappedValue = _mapper(item);
            var mappedAssertion = new ValueAssertion<TMapped>(mappedValue, $"mapped[{index}]");
            try
            {
                _assertion(mappedAssertion);
            }
            catch (Exception ex)
            {
                return Task.FromResult(AssertionResult.Failed($"item at index {index} (mapped by {_mapperDescription}) failed assertion: {ex.Message}"));
            }
            index++;
        }

        return Task.FromResult(AssertionResult.Passed);
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
        EvaluationContext<TCollection> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(TCollection? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("collection was null"));

        TItem? previous = default;
        bool first = true;
        int index = 0;

        foreach (var item in value)
        {
            if (!first && previous != null)
            {
                if (previous.CompareTo(item) > 0)
                    return Task.FromResult(AssertionResult.Failed($"item at index {index} ({item}) is less than previous item ({previous})"));
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
        EvaluationContext<TCollection> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(TCollection? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("collection was null"));

        TItem? previous = default;
        bool first = true;
        int index = 0;

        foreach (var item in value)
        {
            if (!first && previous != null)
            {
                if (previous.CompareTo(item) < 0)
                    return Task.FromResult(AssertionResult.Failed($"item at index {index} ({item}) is greater than previous item ({previous})"));
            }

            previous = item;
            first = false;
            index++;
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be in descending order";
}
