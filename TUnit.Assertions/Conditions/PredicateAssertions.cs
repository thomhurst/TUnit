using System.Text;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Conditions.Helpers;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a value satisfies a predicate function.
/// </summary>
public class SatisfiesAssertion<TValue> : Assertion<TValue>
{
    private readonly Func<TValue?, bool> _predicate;
    private readonly string _predicateDescription;

    public SatisfiesAssertion(
        AssertionContext<TValue> context,
        Func<TValue?, bool> predicate,
        string predicateDescription)
        : base(context)
    {
        _predicate = predicate;
        _predicateDescription = predicateDescription;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (_predicate(value))
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed($"value {value} does not satisfy predicate"));
    }

    protected override string GetExpectation() => $"to satisfy {_predicateDescription}";
}

/// <summary>
/// Asserts that a value is equal to the expected value using either IEquatable or default equality.
/// This is useful for types that implement IEquatable for better performance.
/// </summary>
[AssertionExtension("IsEquatableOrEqualTo")]
public class IsEquatableOrEqualToAssertion<TValue> : ComparerBasedAssertion<TValue, TValue>
{
    private readonly TValue _expected;

    public IsEquatableOrEqualToAssertion(
        AssertionContext<TValue> context,
        TValue expected)
        : base(context)
    {
        _expected = expected;
    }

    public IsEquatableOrEqualToAssertion<TValue> Using(IEqualityComparer<TValue> comparer)
    {
        SetComparer(comparer);
        return this;
    }

    public IsEquatableOrEqualToAssertion<TValue> Using(Func<TValue?, TValue?, bool> equalityPredicate)
    {
        SetComparer(new FuncEqualityComparer<TValue>(equalityPredicate));
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

        if (comparer.Equals(value!, _expected))
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed($"found {value}"));
    }

    protected override string GetExpectation() => $"to be equal to {_expected}";
}
