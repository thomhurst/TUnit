using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Assertion builder for the AssertionBase system that supports proper And/Or chaining
/// </summary>
public class AssertionChainBuilder<T> : AssertionBuilder<T>
{
    private readonly AssertionBase<T> _rootAssertion;
    private readonly ChainType _chainType;

    internal AssertionChainBuilder(Func<Task<T>> actualValueProvider, AssertionBase<T> rootAssertion, ChainType chainType)
        : base(actualValueProvider)
    {
        _rootAssertion = rootAssertion ?? throw new ArgumentNullException(nameof(rootAssertion));
        _chainType = chainType;
    }

    /// <summary>
    /// Creates a custom assertion and chains it with the previous assertion
    /// </summary>
    public new AssertionBase<T> Satisfies(Func<T, bool> predicate, string? failureMessage = null)
    {
        var assertion = new CustomAssertion<T>(ActualValueProvider, predicate, failureMessage);
        return ChainAssertion(assertion);
    }

    /// <summary>
    /// Equals assertion that chains properly
    /// </summary>
    public new AssertionBase<T> IsEqualTo(T expected)
    {
        var assertion = new CustomAssertion<T>(ActualValueProvider,
            actual => actual?.Equals(expected) ?? expected == null,
            $"Expected {expected} but was {{0}}");
        return ChainAssertion(assertion);
    }

    /// <summary>
    /// Not equals assertion that chains properly
    /// </summary>
    public new AssertionBase<T> IsNotEqualTo(T expected)
    {
        var assertion = new CustomAssertion<T>(ActualValueProvider,
            actual => !(actual?.Equals(expected) ?? expected == null),
            $"Expected not {expected} but was {{0}}");
        return ChainAssertion(assertion);
    }

    /// <summary>
    /// Null assertion that chains properly
    /// </summary>
    public new AssertionBase<T> IsNull()
    {
        var assertion = new CustomAssertion<T>(ActualValueProvider,
            actual => actual == null,
            "Expected null but was {0}");
        return ChainAssertion(assertion);
    }

    /// <summary>
    /// Not null assertion that chains properly
    /// </summary>
    public new AssertionBase<T> IsNotNull()
    {
        var assertion = new CustomAssertion<T>(ActualValueProvider,
            actual => actual != null,
            "Expected not null but was null");
        return ChainAssertion(assertion);
    }

    /// <summary>
    /// Collection-specific: Check if collection is empty
    /// </summary>
    public new AssertionBase<T> IsEmpty()
    {
        var assertion = new CustomAssertion<T>(ActualValueProvider,
            actual =>
            {
                if (actual is System.Collections.IEnumerable enumerable)
                {
                    foreach (var _ in enumerable)
                        return false;
                    return true;
                }
                return false;
            },
            "Expected collection to be empty but it contained items");
        return ChainAssertion(assertion);
    }

    /// <summary>
    /// Collection-specific: Check if collection is not empty
    /// </summary>
    public new AssertionBase<T> IsNotEmpty()
    {
        var assertion = new CustomAssertion<T>(ActualValueProvider,
            actual =>
            {
                if (actual is System.Collections.IEnumerable enumerable)
                {
                    foreach (var _ in enumerable)
                        return true;
                    return false;
                }
                return false;
            },
            "Expected collection to be non-empty but it was empty");
        return ChainAssertion(assertion);
    }

    /// <summary>
    /// Collection-specific: Check if collection contains an item
    /// </summary>
    public new AssertionBase<T> Contains(object? item)
    {
        var assertion = new CustomAssertion<T>(ActualValueProvider,
            actual =>
            {
                if (actual is System.Collections.IEnumerable enumerable)
                {
                    foreach (var element in enumerable)
                    {
                        if (object.Equals(element, item))
                            return true;
                    }
                }
                return false;
            },
            $"Expected collection to contain {item} but it didn't");
        return ChainAssertion(assertion);
    }

    /// <summary>
    /// Collection-specific: Check if collection has a specific count
    /// </summary>
    public new AssertionBase<T> HasCount(int expectedCount)
    {
        var assertion = new CustomAssertion<T>(ActualValueProvider,
            actual =>
            {
                if (actual is System.Collections.IEnumerable enumerable)
                {
                    var count = 0;
                    foreach (var _ in enumerable)
                        count++;
                    return count == expectedCount;
                }
                return false;
            },
            $"Expected collection to have {expectedCount} items but had {{0}} items");
        return ChainAssertion(assertion);
    }

    /// <summary>
    /// Boolean-specific: Check if value is true
    /// </summary>
    public new AssertionBase<T> IsTrue()
    {
        if (typeof(T) != typeof(bool))
            throw new InvalidOperationException("IsTrue can only be used with boolean values");

        var boolProvider = async () => {
            var value = await ActualValueProvider();
            return (bool)(object)value!;
        };
        var assertion = new BooleanAssertion(boolProvider, expectedValue: true);
        return ChainBooleanAssertion(assertion);
    }

    /// <summary>
    /// Boolean-specific: Check if value is false
    /// </summary>
    public new AssertionBase<T> IsFalse()
    {
        if (typeof(T) != typeof(bool))
            throw new InvalidOperationException("IsFalse can only be used with boolean values");

        var boolProvider = async () => {
            var value = await ActualValueProvider();
            return (bool)(object)value!;
        };
        var assertion = new BooleanAssertion(boolProvider, expectedValue: false);
        return ChainBooleanAssertion(assertion);
    }

    private AssertionBase<T> ChainAssertion<TAssertion>(TAssertion newAssertion)
        where TAssertion : AssertionBase<T>
    {
        // Set the chain type on the root assertion and chain the new assertion
        _rootAssertion.SetNextChainType(_chainType);
        _rootAssertion.Chain(newAssertion);
        // Return the root assertion for fluent chaining
        return _rootAssertion;
    }

    // Overload for BooleanAssertion when T is bool - return the root of the chain
    private AssertionBase<T> ChainBooleanAssertion(BooleanAssertion newAssertion)
    {
        if (typeof(T) != typeof(bool))
            throw new InvalidOperationException("Can only chain BooleanAssertion when T is bool");

        // Cast to handle the type mismatch
        var rootBoolAssertion = (AssertionBase<bool>)(object)_rootAssertion;

        rootBoolAssertion.SetNextChainType(_chainType);
        rootBoolAssertion.Chain(newAssertion);
        return _rootAssertion; // Return the root of the chain, not the new assertion
    }
}