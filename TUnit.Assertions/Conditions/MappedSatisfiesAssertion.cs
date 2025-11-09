using System.Text;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a mapped value satisfies custom assertions.
/// Maps the source value using a selector, then runs assertions on the mapped value.
/// This version supports both same-type and type-changing assertions (e.g., IsTypeOf).
/// Example: await Assert.That(model).Satisfies(m => m.Name, assert => assert.IsEqualTo("John"));
/// Example: await Assert.That(model).Satisfies(m => m.Value, assert => assert.IsTypeOf<string>());
/// </summary>
public class MappedSatisfiesAssertion<TValue, TMapped> : Assertion<TValue>
{
    private readonly Func<TValue?, TMapped> _selector;
    private readonly Func<ValueAssertion<TMapped>, IAssertion?> _assertions;
    private readonly string _selectorDescription;

    public MappedSatisfiesAssertion(
        AssertionContext<TValue> context,
        Func<TValue?, TMapped> selector,
        Func<ValueAssertion<TMapped>, IAssertion?> assertions,
        string selectorDescription)
        : base(context)
    {
        _selector = selector;
        _assertions = assertions;
        _selectorDescription = selectorDescription;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return AssertionResult.Failed($"threw {exception.GetType().Name}");
        }

        // Map the value
        TMapped mappedValue;
        try
        {
            mappedValue = _selector(value);
        }
        catch (Exception ex)
        {
            return AssertionResult.Failed($"selector threw {ex.GetType().Name}: {ex.Message}");
        }

        // Create an assertion for the mapped value
        var mappedAssertion = new ValueAssertion<TMapped>(mappedValue, _selectorDescription);

        // Run the assertions and execute the resulting assertion chain
        try
        {
            var resultingAssertion = _assertions(mappedAssertion);
            if (resultingAssertion != null)
            {
                await resultingAssertion.AssertAsync();
            }
            return AssertionResult.Passed;
        }
        catch (Exception ex)
        {
            return AssertionResult.Failed($"mapped value did not satisfy assertions: {ex.Message}");
        }
    }

    protected override string GetExpectation() => $"mapped value from {_selectorDescription} to satisfy assertions";
}

/// <summary>
/// Asserts that an async-mapped value satisfies custom assertions.
/// Maps the source value using an async selector, then runs assertions on the mapped value.
/// This version supports both same-type and type-changing assertions (e.g., IsTypeOf).
/// Example: await Assert.That(model).Satisfies(m => m.GetNameAsync(), assert => assert.IsEqualTo("John"));
/// </summary>
public class AsyncMappedSatisfiesAssertion<TValue, TMapped> : Assertion<TValue>
{
    private readonly Func<TValue?, Task<TMapped>> _selector;
    private readonly Func<ValueAssertion<TMapped>, IAssertion?> _assertions;
    private readonly string _selectorDescription;

    public AsyncMappedSatisfiesAssertion(
        AssertionContext<TValue> context,
        Func<TValue?, Task<TMapped>> selector,
        Func<ValueAssertion<TMapped>, IAssertion?> assertions,
        string selectorDescription)
        : base(context)
    {
        _selector = selector;
        _assertions = assertions;
        _selectorDescription = selectorDescription;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return AssertionResult.Failed($"threw {exception.GetType().Name}");
        }

        // Map the value asynchronously
        TMapped mappedValue;
        try
        {
            mappedValue = await _selector(value);
        }
        catch (Exception ex)
        {
            return AssertionResult.Failed($"selector threw {ex.GetType().Name}: {ex.Message}");
        }

        // Create an assertion for the mapped value
        var mappedAssertion = new ValueAssertion<TMapped>(mappedValue, _selectorDescription);

        // Run the assertions and execute the resulting assertion chain
        try
        {
            var resultingAssertion = _assertions(mappedAssertion);
            if (resultingAssertion != null)
            {
                await resultingAssertion.AssertAsync();
            }
            return AssertionResult.Passed;
        }
        catch (Exception ex)
        {
            return AssertionResult.Failed($"mapped value did not satisfy assertions: {ex.Message}");
        }
    }

    protected override string GetExpectation() => $"mapped value from {_selectorDescription} to satisfy assertions";
}
