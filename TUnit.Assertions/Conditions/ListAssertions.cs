using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.Adapters;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that the item at the specified index equals the expected value.
/// </summary>
public class ListHasItemAtAssertion<TList, TItem> : ListAssertionBase<TList, TItem>
    where TList : IList<TItem>
{
    private readonly int _index;
    private readonly TItem _expected;

    public ListHasItemAtAssertion(
        AssertionContext<TList> context,
        int index,
        TItem expected)
        : base(context)
    {
        _index = index;
        _expected = expected;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TList> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("list was null"));
        }

        if (_index < 0 || _index >= metadata.Value.Count)
        {
            return Task.FromResult(AssertionResult.Failed(
                $"index {_index} is out of range (list has {metadata.Value.Count} items)"));
        }

        var actualItem = metadata.Value[_index];
        var comparer = EqualityComparer<TItem>.Default;

        if (comparer.Equals(actualItem, _expected))
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed($"item at index {_index} was {actualItem}"));
    }

    protected override string GetExpectation() => $"to have item {_expected} at index {_index}";
}

/// <summary>
/// Source for asserting on the item at a specific index in a list.
/// This enables patterns like: Assert.That(list).ItemAt(0).IsEqualTo(expected)
/// </summary>
public class ListItemAtSource<TList, TItem> : IAssertionSource<TItem>
    where TList : IList<TItem>
{
    private readonly AssertionContext<TList> _listContext;
    private readonly int _index;

    public AssertionContext<TItem> Context { get; }

    public ListItemAtSource(AssertionContext<TList> listContext, int index)
    {
        _listContext = listContext;
        _index = index;

        // Create a derived context for the item
        var expressionBuilder = new StringBuilder();
        expressionBuilder.Append(_listContext.ExpressionBuilder.ToString());
        Context = new AssertionContext<TItem>((TItem?)default, expressionBuilder);
    }

    /// <summary>
    /// Asserts that the item at the index equals the expected value.
    /// </summary>
    public ListItemAtEqualsAssertion<TList, TItem> IsEqualTo(
        TItem expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _listContext.ExpressionBuilder.Append($".IsEqualTo({expression})");
        return new ListItemAtEqualsAssertion<TList, TItem>(_listContext, _index, expected, negate: false);
    }

    /// <summary>
    /// Asserts that the item at the index does not equal the expected value.
    /// </summary>
    public ListItemAtEqualsAssertion<TList, TItem> IsNotEqualTo(
        TItem expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _listContext.ExpressionBuilder.Append($".IsNotEqualTo({expression})");
        return new ListItemAtEqualsAssertion<TList, TItem>(_listContext, _index, expected, negate: true);
    }

    /// <summary>
    /// Asserts that the item at the index is null.
    /// </summary>
    public ListItemAtNullAssertion<TList, TItem> IsNull()
    {
        _listContext.ExpressionBuilder.Append(".IsNull()");
        return new ListItemAtNullAssertion<TList, TItem>(_listContext, _index, expectNull: true);
    }

    /// <summary>
    /// Asserts that the item at the index is not null.
    /// </summary>
    public ListItemAtNullAssertion<TList, TItem> IsNotNull()
    {
        _listContext.ExpressionBuilder.Append(".IsNotNull()");
        return new ListItemAtNullAssertion<TList, TItem>(_listContext, _index, expectNull: false);
    }

    /// <summary>
    /// Asserts that the item at the index satisfies the given assertion.
    /// </summary>
    public ListItemAtSatisfiesAssertion<TList, TItem> Satisfies(
        Func<IAssertionSource<TItem>, Assertion<TItem>?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
    {
        _listContext.ExpressionBuilder.Append($".Satisfies({expression})");
        return new ListItemAtSatisfiesAssertion<TList, TItem>(_listContext, _index, assertion);
    }

    /// <inheritdoc />
    public TypeOfAssertion<TItem, TExpected> IsTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<TItem, TExpected>(Context);
    }

    /// <inheritdoc />
    public IsNotTypeOfAssertion<TItem, TExpected> IsNotTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsNotTypeOf<{typeof(TExpected).Name}>()");
        return new IsNotTypeOfAssertion<TItem, TExpected>(Context);
    }

    /// <inheritdoc />
    public IsAssignableToAssertion<TExpected, TItem> IsAssignableTo<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableTo<{typeof(TExpected).Name}>()");
        return new IsAssignableToAssertion<TExpected, TItem>(Context);
    }

    /// <inheritdoc />
    public IsNotAssignableToAssertion<TExpected, TItem> IsNotAssignableTo<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableTo<{typeof(TExpected).Name}>()");
        return new IsNotAssignableToAssertion<TExpected, TItem>(Context);
    }

    /// <inheritdoc />
    public IsAssignableFromAssertion<TExpected, TItem> IsAssignableFrom<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableFrom<{typeof(TExpected).Name}>()");
        return new IsAssignableFromAssertion<TExpected, TItem>(Context);
    }

    /// <inheritdoc />
    public IsNotAssignableFromAssertion<TExpected, TItem> IsNotAssignableFrom<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableFrom<{typeof(TExpected).Name}>()");
        return new IsNotAssignableFromAssertion<TExpected, TItem>(Context);
    }
}

/// <summary>
/// Source for asserting on the last item in a list.
/// This enables patterns like: Assert.That(list).LastItem().IsEqualTo(expected)
/// </summary>
public class ListLastItemSource<TList, TItem> : IAssertionSource<TItem>
    where TList : IList<TItem>
{
    private readonly AssertionContext<TList> _listContext;

    public AssertionContext<TItem> Context { get; }

    public ListLastItemSource(AssertionContext<TList> listContext)
    {
        _listContext = listContext;

        // Create a derived context for the item
        var expressionBuilder = new StringBuilder();
        expressionBuilder.Append(_listContext.ExpressionBuilder.ToString());
        Context = new AssertionContext<TItem>((TItem?)default, expressionBuilder);
    }

    /// <summary>
    /// Asserts that the last item equals the expected value.
    /// </summary>
    public ListLastItemEqualsAssertion<TList, TItem> IsEqualTo(
        TItem expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _listContext.ExpressionBuilder.Append($".IsEqualTo({expression})");
        return new ListLastItemEqualsAssertion<TList, TItem>(_listContext, expected, negate: false);
    }

    /// <summary>
    /// Asserts that the last item does not equal the expected value.
    /// </summary>
    public ListLastItemEqualsAssertion<TList, TItem> IsNotEqualTo(
        TItem expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _listContext.ExpressionBuilder.Append($".IsNotEqualTo({expression})");
        return new ListLastItemEqualsAssertion<TList, TItem>(_listContext, expected, negate: true);
    }

    /// <summary>
    /// Asserts that the last item is null.
    /// </summary>
    public ListLastItemNullAssertion<TList, TItem> IsNull()
    {
        _listContext.ExpressionBuilder.Append(".IsNull()");
        return new ListLastItemNullAssertion<TList, TItem>(_listContext, expectNull: true);
    }

    /// <summary>
    /// Asserts that the last item is not null.
    /// </summary>
    public ListLastItemNullAssertion<TList, TItem> IsNotNull()
    {
        _listContext.ExpressionBuilder.Append(".IsNotNull()");
        return new ListLastItemNullAssertion<TList, TItem>(_listContext, expectNull: false);
    }

    /// <summary>
    /// Asserts that the last item satisfies the given assertion.
    /// </summary>
    public ListLastItemSatisfiesAssertion<TList, TItem> Satisfies(
        Func<IAssertionSource<TItem>, Assertion<TItem>?> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
    {
        _listContext.ExpressionBuilder.Append($".Satisfies({expression})");
        return new ListLastItemSatisfiesAssertion<TList, TItem>(_listContext, assertion);
    }

    /// <inheritdoc />
    public TypeOfAssertion<TItem, TExpected> IsTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<TItem, TExpected>(Context);
    }

    /// <inheritdoc />
    public IsNotTypeOfAssertion<TItem, TExpected> IsNotTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsNotTypeOf<{typeof(TExpected).Name}>()");
        return new IsNotTypeOfAssertion<TItem, TExpected>(Context);
    }

    /// <inheritdoc />
    public IsAssignableToAssertion<TExpected, TItem> IsAssignableTo<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableTo<{typeof(TExpected).Name}>()");
        return new IsAssignableToAssertion<TExpected, TItem>(Context);
    }

    /// <inheritdoc />
    public IsNotAssignableToAssertion<TExpected, TItem> IsNotAssignableTo<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableTo<{typeof(TExpected).Name}>()");
        return new IsNotAssignableToAssertion<TExpected, TItem>(Context);
    }

    /// <inheritdoc />
    public IsAssignableFromAssertion<TExpected, TItem> IsAssignableFrom<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableFrom<{typeof(TExpected).Name}>()");
        return new IsAssignableFromAssertion<TExpected, TItem>(Context);
    }

    /// <inheritdoc />
    public IsNotAssignableFromAssertion<TExpected, TItem> IsNotAssignableFrom<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableFrom<{typeof(TExpected).Name}>()");
        return new IsNotAssignableFromAssertion<TExpected, TItem>(Context);
    }
}

/// <summary>
/// Asserts that the item at a specific index equals the expected value.
/// </summary>
public class ListItemAtEqualsAssertion<TList, TItem> : ListAssertionBase<TList, TItem>
    where TList : IList<TItem>
{
    private readonly int _index;
    private readonly TItem _expected;
    private readonly bool _negate;

    public ListItemAtEqualsAssertion(
        AssertionContext<TList> context,
        int index,
        TItem expected,
        bool negate)
        : base(context)
    {
        _index = index;
        _expected = expected;
        _negate = negate;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TList> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("list was null"));
        }

        if (_index < 0 || _index >= metadata.Value.Count)
        {
            return Task.FromResult(AssertionResult.Failed(
                $"index {_index} is out of range (list has {metadata.Value.Count} items)"));
        }

        var actualItem = metadata.Value[_index];
        var comparer = EqualityComparer<TItem>.Default;
        var areEqual = comparer.Equals(actualItem, _expected);

        if (_negate)
        {
            return areEqual
                ? Task.FromResult(AssertionResult.Failed($"item at index {_index} was {actualItem}"))
                : AssertionResult._passedTask;
        }
        else
        {
            return areEqual
                ? AssertionResult._passedTask
                : Task.FromResult(AssertionResult.Failed($"item at index {_index} was {actualItem}"));
        }
    }

    protected override string GetExpectation() =>
        _negate
            ? $"item at index {_index} to not be {_expected}"
            : $"item at index {_index} to be {_expected}";
}

/// <summary>
/// Asserts that the item at a specific index is null or not null.
/// </summary>
public class ListItemAtNullAssertion<TList, TItem> : ListAssertionBase<TList, TItem>
    where TList : IList<TItem>
{
    private readonly int _index;
    private readonly bool _expectNull;

    public ListItemAtNullAssertion(
        AssertionContext<TList> context,
        int index,
        bool expectNull)
        : base(context)
    {
        _index = index;
        _expectNull = expectNull;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TList> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("list was null"));
        }

        if (_index < 0 || _index >= metadata.Value.Count)
        {
            return Task.FromResult(AssertionResult.Failed(
                $"index {_index} is out of range (list has {metadata.Value.Count} items)"));
        }

        var actualItem = metadata.Value[_index];
        var isNull = actualItem is null;

        if (_expectNull)
        {
            return isNull
                ? AssertionResult._passedTask
                : Task.FromResult(AssertionResult.Failed($"item at index {_index} was {actualItem}"));
        }
        else
        {
            return isNull
                ? Task.FromResult(AssertionResult.Failed($"item at index {_index} was null"))
                : AssertionResult._passedTask;
        }
    }

    protected override string GetExpectation() =>
        _expectNull
            ? $"item at index {_index} to be null"
            : $"item at index {_index} to not be null";
}

/// <summary>
/// Asserts that the item at a specific index satisfies a custom assertion.
/// </summary>
public class ListItemAtSatisfiesAssertion<TList, TItem> : ListAssertionBase<TList, TItem>
    where TList : IList<TItem>
{
    private readonly int _index;
    private readonly Func<IAssertionSource<TItem>, Assertion<TItem>?> _assertion;

    public ListItemAtSatisfiesAssertion(
        AssertionContext<TList> context,
        int index,
        Func<IAssertionSource<TItem>, Assertion<TItem>?> assertion)
        : base(context)
    {
        _index = index;
        _assertion = assertion;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<TList> metadata)
    {
        if (metadata.Exception != null)
        {
            return AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}");
        }

        if (metadata.Value == null)
        {
            return AssertionResult.Failed("list was null");
        }

        if (_index < 0 || _index >= metadata.Value.Count)
        {
            return AssertionResult.Failed(
                $"index {_index} is out of range (list has {metadata.Value.Count} items)");
        }

        var actualItem = metadata.Value[_index];
        var itemSource = new ValueAssertion<TItem>(actualItem, $"item[{_index}]");
        var resultingAssertion = _assertion(itemSource);

        if (resultingAssertion != null)
        {
            try
            {
                await resultingAssertion.AssertAsync();
                return AssertionResult.Passed;
            }
            catch (Exception ex)
            {
                return AssertionResult.Failed($"item at index {_index} did not satisfy assertion: {ex.Message}");
            }
        }

        return AssertionResult.Passed;
    }

    protected override string GetExpectation() => $"item at index {_index} to satisfy assertion";
}

/// <summary>
/// Asserts that the last item equals the expected value.
/// </summary>
public class ListLastItemEqualsAssertion<TList, TItem> : ListAssertionBase<TList, TItem>
    where TList : IList<TItem>
{
    private readonly TItem _expected;
    private readonly bool _negate;

    public ListLastItemEqualsAssertion(
        AssertionContext<TList> context,
        TItem expected,
        bool negate)
        : base(context)
    {
        _expected = expected;
        _negate = negate;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TList> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("list was null"));
        }

        if (metadata.Value.Count == 0)
        {
            return Task.FromResult(AssertionResult.Failed("list was empty"));
        }

        var lastItem = metadata.Value[metadata.Value.Count - 1];
        var comparer = EqualityComparer<TItem>.Default;
        var areEqual = comparer.Equals(lastItem, _expected);

        if (_negate)
        {
            return areEqual
                ? Task.FromResult(AssertionResult.Failed($"last item was {lastItem}"))
                : AssertionResult._passedTask;
        }
        else
        {
            return areEqual
                ? AssertionResult._passedTask
                : Task.FromResult(AssertionResult.Failed($"last item was {lastItem}"));
        }
    }

    protected override string GetExpectation() =>
        _negate
            ? $"last item to not be {_expected}"
            : $"last item to be {_expected}";
}

/// <summary>
/// Asserts that the last item is null or not null.
/// </summary>
public class ListLastItemNullAssertion<TList, TItem> : ListAssertionBase<TList, TItem>
    where TList : IList<TItem>
{
    private readonly bool _expectNull;

    public ListLastItemNullAssertion(
        AssertionContext<TList> context,
        bool expectNull)
        : base(context)
    {
        _expectNull = expectNull;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TList> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("list was null"));
        }

        if (metadata.Value.Count == 0)
        {
            return Task.FromResult(AssertionResult.Failed("list was empty"));
        }

        var lastItem = metadata.Value[metadata.Value.Count - 1];
        var isNull = lastItem is null;

        if (_expectNull)
        {
            return isNull
                ? AssertionResult._passedTask
                : Task.FromResult(AssertionResult.Failed($"last item was {lastItem}"));
        }
        else
        {
            return isNull
                ? Task.FromResult(AssertionResult.Failed("last item was null"))
                : AssertionResult._passedTask;
        }
    }

    protected override string GetExpectation() =>
        _expectNull
            ? "last item to be null"
            : "last item to not be null";
}

/// <summary>
/// Asserts that the last item satisfies a custom assertion.
/// </summary>
public class ListLastItemSatisfiesAssertion<TList, TItem> : ListAssertionBase<TList, TItem>
    where TList : IList<TItem>
{
    private readonly Func<IAssertionSource<TItem>, Assertion<TItem>?> _assertion;

    public ListLastItemSatisfiesAssertion(
        AssertionContext<TList> context,
        Func<IAssertionSource<TItem>, Assertion<TItem>?> assertion)
        : base(context)
    {
        _assertion = assertion;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<TList> metadata)
    {
        if (metadata.Exception != null)
        {
            return AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}");
        }

        if (metadata.Value == null)
        {
            return AssertionResult.Failed("list was null");
        }

        if (metadata.Value.Count == 0)
        {
            return AssertionResult.Failed("list was empty");
        }

        var lastItem = metadata.Value[metadata.Value.Count - 1];
        var itemSource = new ValueAssertion<TItem>(lastItem, "lastItem");
        var resultingAssertion = _assertion(itemSource);

        if (resultingAssertion != null)
        {
            try
            {
                await resultingAssertion.AssertAsync();
                return AssertionResult.Passed;
            }
            catch (Exception ex)
            {
                return AssertionResult.Failed($"last item did not satisfy assertion: {ex.Message}");
            }
        }

        return AssertionResult.Passed;
    }

    protected override string GetExpectation() => "last item to satisfy assertion";
}
