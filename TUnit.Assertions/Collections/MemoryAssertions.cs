#if NET5_0_OR_GREATER
using TUnit.Assertions.Abstractions;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Collections;

/// <summary>
/// Asserts that a memory is empty.
/// </summary>
public class MemoryIsEmptyAssertion<TMemory, TItem> : MemoryAssertionBase<TMemory, TItem>
{
    private readonly Func<TMemory, ICollectionAdapter<TItem>> _adapterFactory;

    public MemoryIsEmptyAssertion(
        AssertionContext<TMemory> context,
        Func<TMemory, ICollectionAdapter<TItem>> adapterFactory)
        : base(context)
    {
        _adapterFactory = adapterFactory;
    }

    protected override ICollectionAdapter<TItem> CreateAdapter(TMemory value) => _adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TMemory> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var adapter = _adapterFactory(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckIsEmpty(adapter));
    }

    protected override string GetExpectation() => "to be empty";
}

/// <summary>
/// Asserts that a memory is not empty.
/// </summary>
public class MemoryIsNotEmptyAssertion<TMemory, TItem> : MemoryAssertionBase<TMemory, TItem>
{
    private readonly Func<TMemory, ICollectionAdapter<TItem>> _adapterFactory;

    public MemoryIsNotEmptyAssertion(
        AssertionContext<TMemory> context,
        Func<TMemory, ICollectionAdapter<TItem>> adapterFactory)
        : base(context)
    {
        _adapterFactory = adapterFactory;
    }

    protected override ICollectionAdapter<TItem> CreateAdapter(TMemory value) => _adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TMemory> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var adapter = _adapterFactory(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckIsNotEmpty(adapter));
    }

    protected override string GetExpectation() => "to not be empty";
}

/// <summary>
/// Asserts that a memory contains the expected item.
/// </summary>
public class MemoryContainsAssertion<TMemory, TItem> : MemoryAssertionBase<TMemory, TItem>
{
    private readonly Func<TMemory, ICollectionAdapter<TItem>> _adapterFactory;
    private readonly TItem _expected;
    private readonly IEqualityComparer<TItem>? _comparer;

    public MemoryContainsAssertion(
        AssertionContext<TMemory> context,
        Func<TMemory, ICollectionAdapter<TItem>> adapterFactory,
        TItem expected,
        IEqualityComparer<TItem>? comparer = null)
        : base(context)
    {
        _adapterFactory = adapterFactory;
        _expected = expected;
        _comparer = comparer;
    }

    protected override ICollectionAdapter<TItem> CreateAdapter(TMemory value) => _adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TMemory> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var adapter = _adapterFactory(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckContains(adapter, _expected, _comparer));
    }

    protected override string GetExpectation() => $"to contain {_expected}";
}

/// <summary>
/// Asserts that a memory does not contain the expected item.
/// </summary>
public class MemoryDoesNotContainAssertion<TMemory, TItem> : MemoryAssertionBase<TMemory, TItem>
{
    private readonly Func<TMemory, ICollectionAdapter<TItem>> _adapterFactory;
    private readonly TItem _expected;
    private readonly IEqualityComparer<TItem>? _comparer;

    public MemoryDoesNotContainAssertion(
        AssertionContext<TMemory> context,
        Func<TMemory, ICollectionAdapter<TItem>> adapterFactory,
        TItem expected,
        IEqualityComparer<TItem>? comparer = null)
        : base(context)
    {
        _adapterFactory = adapterFactory;
        _expected = expected;
        _comparer = comparer;
    }

    protected override ICollectionAdapter<TItem> CreateAdapter(TMemory value) => _adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TMemory> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var adapter = _adapterFactory(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckDoesNotContain(adapter, _expected, _comparer));
    }

    protected override string GetExpectation() => $"to not contain {_expected}";
}

/// <summary>
/// Asserts that a memory has exactly one item.
/// </summary>
public class MemoryHasSingleItemAssertion<TMemory, TItem> : MemoryAssertionBase<TMemory, TItem>
{
    private readonly Func<TMemory, ICollectionAdapter<TItem>> _adapterFactory;

    public MemoryHasSingleItemAssertion(
        AssertionContext<TMemory> context,
        Func<TMemory, ICollectionAdapter<TItem>> adapterFactory)
        : base(context)
    {
        _adapterFactory = adapterFactory;
    }

    protected override ICollectionAdapter<TItem> CreateAdapter(TMemory value) => _adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TMemory> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var adapter = _adapterFactory(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckHasSingleItem(adapter));
    }

    protected override string GetExpectation() => "to have a single item";
}

/// <summary>
/// Asserts that all items in a memory satisfy the predicate.
/// </summary>
public class MemoryAllAssertion<TMemory, TItem> : MemoryAssertionBase<TMemory, TItem>
{
    private readonly Func<TMemory, ICollectionAdapter<TItem>> _adapterFactory;
    private readonly Func<TItem, bool> _predicate;
    private readonly string _predicateExpression;

    public MemoryAllAssertion(
        AssertionContext<TMemory> context,
        Func<TMemory, ICollectionAdapter<TItem>> adapterFactory,
        Func<TItem, bool> predicate,
        string predicateExpression)
        : base(context)
    {
        _adapterFactory = adapterFactory;
        _predicate = predicate;
        _predicateExpression = predicateExpression;
    }

    protected override ICollectionAdapter<TItem> CreateAdapter(TMemory value) => _adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TMemory> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var adapter = _adapterFactory(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckAll(adapter, _predicate, _predicateExpression));
    }

    protected override string GetExpectation() => $"all items to satisfy {_predicateExpression}";
}

/// <summary>
/// Asserts that any item in a memory satisfies the predicate.
/// </summary>
public class MemoryAnyAssertion<TMemory, TItem> : MemoryAssertionBase<TMemory, TItem>
{
    private readonly Func<TMemory, ICollectionAdapter<TItem>> _adapterFactory;
    private readonly Func<TItem, bool> _predicate;

    public MemoryAnyAssertion(
        AssertionContext<TMemory> context,
        Func<TMemory, ICollectionAdapter<TItem>> adapterFactory,
        Func<TItem, bool> predicate)
        : base(context)
    {
        _adapterFactory = adapterFactory;
        _predicate = predicate;
    }

    protected override ICollectionAdapter<TItem> CreateAdapter(TMemory value) => _adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TMemory> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var adapter = _adapterFactory(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckAny(adapter, _predicate));
    }

    protected override string GetExpectation() => "any item to satisfy the condition";
}

/// <summary>
/// Asserts that a memory is in ascending order.
/// </summary>
public class MemoryIsInOrderAssertion<TMemory, TItem> : MemoryAssertionBase<TMemory, TItem>
{
    private readonly Func<TMemory, ICollectionAdapter<TItem>> _adapterFactory;
    private readonly IComparer<TItem>? _comparer;

    public MemoryIsInOrderAssertion(
        AssertionContext<TMemory> context,
        Func<TMemory, ICollectionAdapter<TItem>> adapterFactory,
        IComparer<TItem>? comparer = null)
        : base(context)
    {
        _adapterFactory = adapterFactory;
        _comparer = comparer;
    }

    protected override ICollectionAdapter<TItem> CreateAdapter(TMemory value) => _adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TMemory> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var adapter = _adapterFactory(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckIsInOrder(adapter, _comparer));
    }

    protected override string GetExpectation() => "to be in ascending order";
}

/// <summary>
/// Asserts that a memory is in descending order.
/// </summary>
public class MemoryIsInDescendingOrderAssertion<TMemory, TItem> : MemoryAssertionBase<TMemory, TItem>
{
    private readonly Func<TMemory, ICollectionAdapter<TItem>> _adapterFactory;
    private readonly IComparer<TItem>? _comparer;

    public MemoryIsInDescendingOrderAssertion(
        AssertionContext<TMemory> context,
        Func<TMemory, ICollectionAdapter<TItem>> adapterFactory,
        IComparer<TItem>? comparer = null)
        : base(context)
    {
        _adapterFactory = adapterFactory;
        _comparer = comparer;
    }

    protected override ICollectionAdapter<TItem> CreateAdapter(TMemory value) => _adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TMemory> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var adapter = _adapterFactory(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckIsInDescendingOrder(adapter, _comparer));
    }

    protected override string GetExpectation() => "to be in descending order";
}

/// <summary>
/// Asserts that all items in a memory are distinct.
/// </summary>
public class MemoryHasDistinctItemsAssertion<TMemory, TItem> : MemoryAssertionBase<TMemory, TItem>
{
    private readonly Func<TMemory, ICollectionAdapter<TItem>> _adapterFactory;
    private readonly IEqualityComparer<TItem>? _comparer;

    public MemoryHasDistinctItemsAssertion(
        AssertionContext<TMemory> context,
        Func<TMemory, ICollectionAdapter<TItem>> adapterFactory,
        IEqualityComparer<TItem>? comparer = null)
        : base(context)
    {
        _adapterFactory = adapterFactory;
        _comparer = comparer;
    }

    protected override ICollectionAdapter<TItem> CreateAdapter(TMemory value) => _adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TMemory> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var adapter = _adapterFactory(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckHasDistinctItems(adapter, _comparer));
    }

    protected override string GetExpectation() => "to have distinct items";
}

/// <summary>
/// Provides count assertions for memory types.
/// </summary>
public class MemoryCountSource<TMemory, TItem>
{
    private readonly AssertionContext<TMemory> _context;
    private readonly Func<TMemory, ICollectionAdapter<TItem>> _adapterFactory;

    public MemoryCountSource(
        AssertionContext<TMemory> context,
        Func<TMemory, ICollectionAdapter<TItem>> adapterFactory)
    {
        _context = context;
        _adapterFactory = adapterFactory;
    }

    /// <summary>
    /// Asserts that the count is equal to the expected value.
    /// </summary>
    public MemoryCountEqualsAssertion<TMemory, TItem> IsEqualTo(
        int expected,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _context.ExpressionBuilder.Append($".IsEqualTo({expression})");
        return new MemoryCountEqualsAssertion<TMemory, TItem>(
            _context, _adapterFactory, expected, MemoryCountComparison.Equal);
    }

    /// <summary>
    /// Asserts that the count is not equal to the expected value.
    /// </summary>
    public MemoryCountEqualsAssertion<TMemory, TItem> IsNotEqualTo(
        int expected,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _context.ExpressionBuilder.Append($".IsNotEqualTo({expression})");
        return new MemoryCountEqualsAssertion<TMemory, TItem>(
            _context, _adapterFactory, expected, MemoryCountComparison.NotEqual);
    }

    /// <summary>
    /// Asserts that the count is greater than the expected value.
    /// </summary>
    public MemoryCountEqualsAssertion<TMemory, TItem> IsGreaterThan(
        int expected,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _context.ExpressionBuilder.Append($".IsGreaterThan({expression})");
        return new MemoryCountEqualsAssertion<TMemory, TItem>(
            _context, _adapterFactory, expected, MemoryCountComparison.GreaterThan);
    }

    /// <summary>
    /// Asserts that the count is greater than or equal to the expected value.
    /// </summary>
    public MemoryCountEqualsAssertion<TMemory, TItem> IsGreaterThanOrEqualTo(
        int expected,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _context.ExpressionBuilder.Append($".IsGreaterThanOrEqualTo({expression})");
        return new MemoryCountEqualsAssertion<TMemory, TItem>(
            _context, _adapterFactory, expected, MemoryCountComparison.GreaterThanOrEqual);
    }

    /// <summary>
    /// Asserts that the count is less than the expected value.
    /// </summary>
    public MemoryCountEqualsAssertion<TMemory, TItem> IsLessThan(
        int expected,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _context.ExpressionBuilder.Append($".IsLessThan({expression})");
        return new MemoryCountEqualsAssertion<TMemory, TItem>(
            _context, _adapterFactory, expected, MemoryCountComparison.LessThan);
    }

    /// <summary>
    /// Asserts that the count is less than or equal to the expected value.
    /// </summary>
    public MemoryCountEqualsAssertion<TMemory, TItem> IsLessThanOrEqualTo(
        int expected,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _context.ExpressionBuilder.Append($".IsLessThanOrEqualTo({expression})");
        return new MemoryCountEqualsAssertion<TMemory, TItem>(
            _context, _adapterFactory, expected, MemoryCountComparison.LessThanOrEqual);
    }

    /// <summary>
    /// Asserts that the count is zero.
    /// </summary>
    public MemoryCountEqualsAssertion<TMemory, TItem> IsZero()
    {
        _context.ExpressionBuilder.Append(".IsZero()");
        return new MemoryCountEqualsAssertion<TMemory, TItem>(
            _context, _adapterFactory, 0, MemoryCountComparison.Equal);
    }

    /// <summary>
    /// Asserts that the count is positive (greater than zero).
    /// </summary>
    public MemoryCountEqualsAssertion<TMemory, TItem> IsPositive()
    {
        _context.ExpressionBuilder.Append(".IsPositive()");
        return new MemoryCountEqualsAssertion<TMemory, TItem>(
            _context, _adapterFactory, 0, MemoryCountComparison.GreaterThan);
    }
}

internal enum MemoryCountComparison
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual
}

/// <summary>
/// Memory-aware count assertion that preserves the memory type for further chaining.
/// </summary>
public class MemoryCountEqualsAssertion<TMemory, TItem> : MemoryAssertionBase<TMemory, TItem>
{
    private readonly Func<TMemory, ICollectionAdapter<TItem>> _adapterFactory;
    private readonly int _expected;
    private readonly MemoryCountComparison _comparison;

    internal MemoryCountEqualsAssertion(
        AssertionContext<TMemory> context,
        Func<TMemory, ICollectionAdapter<TItem>> adapterFactory,
        int expected,
        MemoryCountComparison comparison)
        : base(context)
    {
        _adapterFactory = adapterFactory;
        _expected = expected;
        _comparison = comparison;
    }

    protected override ICollectionAdapter<TItem> CreateAdapter(TMemory value) => _adapterFactory(value);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TMemory> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var adapter = _adapterFactory(metadata.Value);
        var actualCount = adapter.Count;

        var passed = _comparison switch
        {
            MemoryCountComparison.Equal => actualCount == _expected,
            MemoryCountComparison.NotEqual => actualCount != _expected,
            MemoryCountComparison.GreaterThan => actualCount > _expected,
            MemoryCountComparison.GreaterThanOrEqual => actualCount >= _expected,
            MemoryCountComparison.LessThan => actualCount < _expected,
            MemoryCountComparison.LessThanOrEqual => actualCount <= _expected,
            _ => false
        };

        if (passed)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"found {actualCount}"));
    }

    protected override string GetExpectation()
    {
        return _comparison switch
        {
            MemoryCountComparison.Equal => $"to have count equal to {_expected}",
            MemoryCountComparison.NotEqual => $"to have count not equal to {_expected}",
            MemoryCountComparison.GreaterThan => $"to have count greater than {_expected}",
            MemoryCountComparison.GreaterThanOrEqual => $"to have count greater than or equal to {_expected}",
            MemoryCountComparison.LessThan => $"to have count less than {_expected}",
            MemoryCountComparison.LessThanOrEqual => $"to have count less than or equal to {_expected}",
            _ => $"to have count {_expected}"
        };
    }
}
#endif
