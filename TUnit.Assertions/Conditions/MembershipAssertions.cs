using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a value is in a collection.
/// </summary>
public class IsInAssertion<TValue> : Assertion<TValue>
{
    private readonly IEnumerable<TValue> _collection;
    private IEqualityComparer<TValue>? _comparer;

    public IsInAssertion(
        EvaluationContext<TValue> context,
        IEnumerable<TValue> collection,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _collection = collection;
    }

    public IsInAssertion<TValue> Using(IEqualityComparer<TValue> comparer)
    {
        _comparer = comparer;
        ExpressionBuilder.Append($".Using({comparer.GetType().Name})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        var comparer = _comparer ?? EqualityComparer<TValue>.Default;

        foreach (var item in _collection)
        {
            if (comparer.Equals(value!, item))
                return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"value {value} was not found in collection"));
    }

    protected override string GetExpectation() => "to be in collection";
}

/// <summary>
/// Asserts that a value is NOT in a collection.
/// </summary>
public class IsNotInAssertion<TValue> : Assertion<TValue>
{
    private readonly IEnumerable<TValue> _collection;
    private IEqualityComparer<TValue>? _comparer;

    public IsNotInAssertion(
        EvaluationContext<TValue> context,
        IEnumerable<TValue> collection,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _collection = collection;
    }

    public IsNotInAssertion<TValue> Using(IEqualityComparer<TValue> comparer)
    {
        _comparer = comparer;
        ExpressionBuilder.Append($".Using({comparer.GetType().Name})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        var comparer = _comparer ?? EqualityComparer<TValue>.Default;

        foreach (var item in _collection)
        {
            if (comparer.Equals(value!, item))
                return Task.FromResult(AssertionResult.Failed($"value {value} was found in collection"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be in collection";
}
