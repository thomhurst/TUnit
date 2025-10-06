using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a dictionary contains a specific key.
/// </summary>
public class DictionaryContainsKeyAssertion<TKey, TValue> : Assertion<IReadOnlyDictionary<TKey, TValue>>
{
    private readonly TKey _expectedKey;

    public DictionaryContainsKeyAssertion(
        EvaluationContext<IReadOnlyDictionary<TKey, TValue>> context,
        TKey expectedKey,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _expectedKey = expectedKey;
    }

    protected override Task<AssertionResult> CheckAsync(IReadOnlyDictionary<TKey, TValue>? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("dictionary was null"));

        if (value.ContainsKey(_expectedKey))
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"key {_expectedKey} not found"));
    }

    protected override string GetExpectation() => $"to contain key {_expectedKey}";
}

/// <summary>
/// Asserts that a dictionary does NOT contain a specific key.
/// </summary>
public class DictionaryDoesNotContainKeyAssertion<TKey, TValue> : Assertion<IReadOnlyDictionary<TKey, TValue>>
{
    private readonly TKey _expectedKey;

    public DictionaryDoesNotContainKeyAssertion(
        EvaluationContext<IReadOnlyDictionary<TKey, TValue>> context,
        TKey expectedKey,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _expectedKey = expectedKey;
    }

    protected override Task<AssertionResult> CheckAsync(IReadOnlyDictionary<TKey, TValue>? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("dictionary was null"));

        if (!value.ContainsKey(_expectedKey))
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"key {_expectedKey} was found"));
    }

    protected override string GetExpectation() => $"to not contain key {_expectedKey}";
}
