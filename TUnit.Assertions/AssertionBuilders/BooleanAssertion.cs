using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
///  boolean assertion with lazy evaluation
/// </summary>
public class BooleanAssertion : AssertionBase<bool>
{
    private readonly bool _expectedValue;

    public BooleanAssertion(Func<Task<bool>> actualValueProvider, bool expectedValue)
        : base(actualValueProvider)
    {
        _expectedValue = expectedValue;
    }

    public BooleanAssertion(Func<bool> actualValueProvider, bool expectedValue)
        : base(actualValueProvider)
    {
        _expectedValue = expectedValue;
    }

    public BooleanAssertion(bool actualValue, bool expectedValue)
        : base(actualValue)
    {
        _expectedValue = expectedValue;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var actual = await GetActualValueAsync();

        if (actual == _expectedValue)
        {
            // For chained assertions, we still need to know what this assertion expected
            // even when it passes, so we include the expectation in a special passed result
            var expectedMessage = _expectedValue ? "Expected true but was false" : "Expected false but was true";
            return AssertionResult.Pass(expectedMessage);
        }

        // Use appropriate error message
        if (_expectedValue)
        {
            return AssertionResult.Fail("Expected true but was false");
        }
        else
        {
            return AssertionResult.Fail("Expected false but was true");
        }
    }

    // Expose expected value for chain building
    internal bool ExpectedValue => _expectedValue;

    /// <summary>
    /// Check if this assertion has chained assertions (will be overridden by complex chain logic)
    /// </summary>
    private bool HasChainedAssertions()
    {
        // This is a simplified check - in a real implementation, this would be passed from the chain context
        // For now, assume standalone assertions should get rich messages
        return false;
    }
}