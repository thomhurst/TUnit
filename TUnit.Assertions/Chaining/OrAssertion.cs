using TUnit.Assertions.Core;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.Chaining;

/// <summary>
/// Combines two assertions with Or logic - at least one must pass.
/// Used internally by Or chaining. Most users won't interact with this directly.
/// </summary>
public class OrAssertion<TValue> : Assertion<TValue>
{
    private readonly Assertion<TValue> _first;
    private readonly Assertion<TValue> _second;

    public OrAssertion(
        Assertion<TValue> first,
        Assertion<TValue> second)
        : base(((IAssertionSource<TValue>)first).Context)
    {
        _first = first ?? throw new ArgumentNullException(nameof(first));
        _second = second ?? throw new ArgumentNullException(nameof(second));
    }

    /// <summary>
    /// Throws when attempting to mix And with Or operators.
    /// </summary>
    public new AndContinuation<TValue> And => throw new MixedAndOrAssertionsException();

    public override async Task<TValue?> AssertAsync()
    {
        Exception? firstException = null;

        try
        {
            // Try first assertion
            return await _first.AssertAsync();
        }
        catch (AssertionException ex)
        {
            // First failed, try second
            firstException = ex;
        }

        try
        {
            return await _second.AssertAsync();
        }
        catch (AssertionException ex)
        {
            // Both failed - throw combined exception
            throw new AssertionException(
                $"""
                Neither assertion passed:
                  First: {firstException.Message}
                  Second: {ex.Message}
                """);
        }
    }

    protected override string GetExpectation() => "either condition";
}
