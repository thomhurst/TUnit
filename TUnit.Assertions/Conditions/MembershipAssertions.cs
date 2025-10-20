using System.Text;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a value is in a collection.
/// </summary>
[AssertionExtension("IsIn")]
public class IsInAssertion<TValue> : ComparerBasedAssertion<TValue, TValue>
{
    private readonly IEnumerable<TValue> _collection;

    public IsInAssertion(
        AssertionContext<TValue> context,
        IEnumerable<TValue> collection)
        : base(context)
    {
        _collection = collection;
    }

    public IsInAssertion<TValue> Using(IEqualityComparer<TValue> comparer)
    {
        SetComparer(comparer);
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        var comparer = GetComparer();

        foreach (var item in _collection)
        {
            if (comparer.Equals(value!, item))
            {
                return Task.FromResult(AssertionResult.Passed);
            }
        }

        return Task.FromResult(AssertionResult.Failed($"value {value} was not found in collection"));
    }

    protected override string GetExpectation() => "to be in collection";
}

/// <summary>
/// Asserts that a value is NOT in a collection.
/// </summary>
[AssertionExtension("IsNotIn")]
public class IsNotInAssertion<TValue> : ComparerBasedAssertion<TValue, TValue>
{
    private readonly IEnumerable<TValue> _collection;

    public IsNotInAssertion(
        AssertionContext<TValue> context,
        IEnumerable<TValue> collection)
        : base(context)
    {
        _collection = collection;
    }

    public IsNotInAssertion<TValue> Using(IEqualityComparer<TValue> comparer)
    {
        SetComparer(comparer);
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        var comparer = GetComparer();

        foreach (var item in _collection)
        {
            if (comparer.Equals(value!, item))
            {
                return Task.FromResult(AssertionResult.Failed($"value {value} was found in collection"));
            }
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be in collection";
}
