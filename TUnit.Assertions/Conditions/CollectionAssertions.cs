using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.Abstractions;
using TUnit.Assertions.Adapters;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Collections;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a collection/enumerable is empty.
/// Delegates to CollectionChecks for the actual logic.
/// </summary>
public class CollectionIsEmptyAssertion<TCollection, TItem> : Sources.CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    public CollectionIsEmptyAssertion(
        AssertionContext<TCollection> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("collection was null"));
        }

        var adapter = new EnumerableAdapter<TItem>(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckIsEmpty(adapter));
    }

    protected override string GetExpectation() => "to be empty";
}

/// <summary>
/// Asserts that a collection/enumerable is NOT empty.
/// Delegates to CollectionChecks for the actual logic.
/// </summary>
public class CollectionIsNotEmptyAssertion<TCollection, TItem> : Sources.CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    public CollectionIsNotEmptyAssertion(
        AssertionContext<TCollection> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("collection was null"));
        }

        var adapter = new EnumerableAdapter<TItem>(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckIsNotEmpty(adapter));
    }

    protected override string GetExpectation() => "to not be empty";
}

/// <summary>
/// Asserts that a collection contains the expected item.
/// Delegates to CollectionChecks for the actual logic.
/// </summary>
[AssertionExtension("Contains")]
public class CollectionContainsAssertion<TCollection, TItem> : Sources.CollectionAssertionBase<TCollection, TItem>
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
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var adapter = new EnumerableAdapter<TItem>(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckContains(adapter, _expected, _comparer));
    }

    protected override string GetExpectation() => $"to contain {_expected}";
}

/// <summary>
/// Asserts that a collection does NOT contain the expected item.
/// Delegates to CollectionChecks for the actual logic.
/// </summary>
[AssertionExtension("DoesNotContain")]
public class CollectionDoesNotContainAssertion<TCollection, TItem> : Sources.CollectionAssertionBase<TCollection, TItem>
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
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var adapter = new EnumerableAdapter<TItem>(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckDoesNotContain(adapter, _expected, _comparer));
    }

    protected override string GetExpectation() => $"to not contain {_expected}";
}

/// <summary>
/// Asserts that a collection does NOT contain any item matching the predicate.
/// Delegates to CollectionChecks for the actual logic.
/// </summary>
[AssertionExtension("DoesNotContain")]
public class CollectionDoesNotContainPredicateAssertion<TCollection, TItem> : Sources.CollectionAssertionBase<TCollection, TItem>
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
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var adapter = new EnumerableAdapter<TItem>(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckDoesNotContainPredicate(adapter, _predicate));
    }

    protected override string GetExpectation() => $"to not contain any item matching {_predicateDescription}";
}

/// <summary>
/// Asserts that a collection has a specific count/length.
/// Delegates to CollectionChecks for the actual logic.
/// </summary>
public class CollectionCountAssertion<TCollection, TItem> : Sources.CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    private readonly int _expectedCount;

    public CollectionCountAssertion(
        AssertionContext<TCollection> context,
        int expectedCount)
        : base(context)
    {
        _expectedCount = expectedCount;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("collection was null"));
        }

        var adapter = new EnumerableAdapter<TItem>(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckCount(adapter, _expectedCount));
    }

    protected override string GetExpectation() => $"to have count {_expectedCount}";
}

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

/// <summary>
/// Asserts that all items in a collection satisfy a predicate.
/// Delegates to CollectionChecks for the actual logic.
/// </summary>
[AssertionExtension("All")]
public class CollectionAllAssertion<TCollection, TItem> : Sources.CollectionAssertionBase<TCollection, TItem>
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
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var adapter = new EnumerableAdapter<TItem>(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckAll(adapter, _predicate, _predicateDescription));
    }

    protected override string GetExpectation() => $"all items to satisfy {_predicateDescription}";
}

/// <summary>
/// Asserts that at least one item in a collection satisfies a predicate.
/// Delegates to CollectionChecks for the actual logic.
/// </summary>
[AssertionExtension("Any")]
public class CollectionAnyAssertion<TCollection, TItem> : Sources.CollectionAssertionBase<TCollection, TItem>
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
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var adapter = new EnumerableAdapter<TItem>(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckAny(adapter, _predicate));
    }

    protected override string GetExpectation() => $"at least one item to satisfy {_predicateDescription}";
}

/// <summary>
/// Asserts that a collection contains exactly one item.
/// When awaited, returns the single item for further assertions.
/// Delegates to CollectionChecks for the count check, but captures the item for return.
/// </summary>
[AssertionExtension("HasSingleItem")]
public class HasSingleItemAssertion<TCollection, TItem> : Sources.CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    private TItem? _singleItem;

    public HasSingleItemAssertion(
        AssertionContext<TCollection> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var adapter = new EnumerableAdapter<TItem>(metadata.Value);
        var result = CollectionChecks.CheckHasSingleItem(adapter);

        if (result.IsPassed)
        {
            // Capture the single item for GetAwaiter
            _singleItem = adapter.AsEnumerable().First();
        }

        return Task.FromResult(result);
    }

    protected override string GetExpectation() => "to have exactly one item";

    /// <summary>
    /// Enables await syntax that returns the single item.
    /// This allows both chaining (.And) and item capture (await).
    /// </summary>
    public new System.Runtime.CompilerServices.TaskAwaiter<TItem> GetAwaiter()
    {
        return ExecuteAndReturnItemAsync().GetAwaiter();
    }

    private async Task<TItem> ExecuteAndReturnItemAsync()
    {
        await AssertAsync();

        return _singleItem!;
    }
}

/// <summary>
/// Asserts that a collection contains an item matching the predicate.
/// When awaited, returns the found item for further assertions.
/// Delegates to CollectionChecks for the actual logic.
/// </summary>
[AssertionExtension("Contains")]
public class CollectionContainsPredicateAssertion<TCollection, TItem> : Sources.CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    private readonly Func<TItem, bool> _predicate;
    private TItem? _foundItem;

    public CollectionContainsPredicateAssertion(
        AssertionContext<TCollection> context,
        Func<TItem, bool> predicate)
        : base(context)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("collection was null"));
        }

        var adapter = new EnumerableAdapter<TItem>(metadata.Value);
        var result = CollectionChecks.CheckContainsPredicate(adapter, _predicate, out _foundItem);
        return Task.FromResult(result);
    }

    protected override string GetExpectation() => "to contain item matching predicate";

    /// <summary>
    /// Enables await syntax that returns the found item.
    /// This allows both chaining (.And) and item capture (await).
    /// </summary>
    public new System.Runtime.CompilerServices.TaskAwaiter<TItem> GetAwaiter()
    {
        return ExecuteAndReturnItemAsync().GetAwaiter();
    }

    private async Task<TItem> ExecuteAndReturnItemAsync()
    {
        await AssertAsync();

        return _foundItem!;
    }
}

/// <summary>
/// Asserts that all items in the collection satisfy a custom assertion.
/// Inherits from CollectionAssertionBase to enable chaining of collection methods.
/// </summary>
public class CollectionAllSatisfyAssertion<TCollection, TItem> : Sources.CollectionAssertionBase<TCollection, TItem>
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
/// Inherits from CollectionAssertionBase to enable chaining of collection methods.
/// </summary>
public class CollectionAllSatisfyMappedAssertion<TCollection, TItem, TMapped> : Sources.CollectionAssertionBase<TCollection, TItem>
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
/// Delegates to CollectionChecks for the actual logic.
/// </summary>
[AssertionExtension("IsInOrder")]
public class CollectionIsInOrderAssertion<TCollection, TItem> : Sources.CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    public CollectionIsInOrderAssertion(
        AssertionContext<TCollection> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("collection was null"));
        }

        var adapter = new EnumerableAdapter<TItem>(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckIsInOrder(adapter));
    }

    protected override string GetExpectation() => "to be in ascending order";
}

/// <summary>
/// Asserts that a collection is in descending order.
/// Delegates to CollectionChecks for the actual logic.
/// </summary>
[AssertionExtension("IsInDescendingOrder")]
public class CollectionIsInDescendingOrderAssertion<TCollection, TItem> : Sources.CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    public CollectionIsInDescendingOrderAssertion(
        AssertionContext<TCollection> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("collection was null"));
        }

        var adapter = new EnumerableAdapter<TItem>(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckIsInDescendingOrder(adapter));
    }

    protected override string GetExpectation() => "to be in descending order";
}

/// <summary>
/// Asserts that a collection is ordered by a key selector in ascending order.
/// Inherits from CollectionAssertionBase to enable chaining of collection methods.
/// Available as an instance method on CollectionAssertionBase for proper type inference.
/// </summary>
public class CollectionIsOrderedByAssertion<TCollection, TItem, TKey> : Sources.CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    private readonly Func<TItem, TKey> _keySelector;
    private readonly IComparer<TKey>? _comparer;

    public CollectionIsOrderedByAssertion(
        AssertionContext<TCollection> context,
        Func<TItem, TKey> keySelector,
        IComparer<TKey>? comparer = null)
        : base(context)
    {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
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
            return Task.FromResult(AssertionResult.Failed("collection was null"));
        }

        var comparer = _comparer ?? Comparer<TKey>.Default;
        var enumerated = value.ToArray();
        var ordered = enumerated.OrderBy(_keySelector, comparer).ToArray();

        if (enumerated.SequenceEqual(ordered))
        {
            return AssertionResult._passedTask;
        }

        for (int i = 1; i < enumerated.Length; i++)
        {
            var prevKey = _keySelector(enumerated[i - 1]);
            var currKey = _keySelector(enumerated[i]);

            if (comparer.Compare(prevKey, currKey) > 0)
            {
                return Task.FromResult(AssertionResult.Failed($"item at index {i} is out of order (key: {currKey}) compared to previous item (key: {prevKey})"));
            }
        }

        return AssertionResult._passedTask;
    }

    protected override string GetExpectation() => "to be ordered by key selector in ascending order";
}

/// <summary>
/// Asserts that a collection is ordered by a key selector in descending order.
/// Inherits from CollectionAssertionBase to enable chaining of collection methods.
/// Available as an instance method on CollectionAssertionBase for proper type inference.
/// </summary>
public class CollectionIsOrderedByDescendingAssertion<TCollection, TItem, TKey> : Sources.CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    private readonly Func<TItem, TKey> _keySelector;
    private readonly IComparer<TKey>? _comparer;

    public CollectionIsOrderedByDescendingAssertion(
        AssertionContext<TCollection> context,
        Func<TItem, TKey> keySelector,
        IComparer<TKey>? comparer = null)
        : base(context)
    {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
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
            return Task.FromResult(AssertionResult.Failed("collection was null"));
        }

        var comparer = _comparer ?? Comparer<TKey>.Default;
        var enumerated = value.ToArray();
        var ordered = enumerated.OrderByDescending(_keySelector, comparer).ToArray();

        if (enumerated.SequenceEqual(ordered))
        {
            return AssertionResult._passedTask;
        }

        for (int i = 1; i < enumerated.Length; i++)
        {
            var prevKey = _keySelector(enumerated[i - 1]);
            var currKey = _keySelector(enumerated[i]);

            if (comparer.Compare(prevKey, currKey) < 0)
            {
                return Task.FromResult(AssertionResult.Failed($"item at index {i} is out of order (key: {currKey}) compared to previous item (key: {prevKey})"));
            }
        }

        return AssertionResult._passedTask;
    }

    protected override string GetExpectation() => "to be ordered by key selector in descending order";
}
